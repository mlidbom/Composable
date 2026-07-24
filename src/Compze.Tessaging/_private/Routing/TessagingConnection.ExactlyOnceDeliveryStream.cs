using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus._private.Outbox;
using Compze.Threading.ResourceAccess;
using Compze.Tessaging._private.Transport;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging._private.Routing;

partial class TessagingConnection
{
   ///<summary>The connection's exactly-once delivery stream: the durable, head-of-line send queue backed by the outbox's storage.<br/>
   /// The queue is keyed and ordered by each tessage's delivery stream sequence number (see <see cref="Compze.Tessaging._internal.Transport.DeliveryStreamPosition"/>),<br/>
   /// so the send order is the pair's stream order by construction — whatever order commit hooks and the recovery backlog load<br/>
   /// happened to enqueue in, and with the one legitimate double-enqueue (a commit hook racing the backlog load) collapsing to a<br/>
   /// single entry. The loop does not look past the lowest-sequenced tessage until it is delivered and acknowledged, retrying a<br/>
   /// failed delivery with backoff; the storage bookkeeping (delivered / failed) and the recovery at start — reloading the<br/>
   /// endpoint's undelivered backlog — are what make the guarantee and the ordering survive restarts<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). A connection carries this stream exactly when the endpoint<br/>
   /// wires the outbox, whose registration grants the router the storage.</summary>
   internal class ExactlyOnceDeliveryStream : IDisposable
   {
      ///<summary>Creates the <see cref="ExactlyOnceDeliveryStream"/> each connection carries. Registered as a component-set member<br/>
      /// by the outbox's wiring, which owns the storage backing every stream: the set's emptiness is what tells the router that the<br/>
      /// endpoint wires no exactly-once delivery, so its connections carry no such stream — the same wiring-supplies-the-legs idiom<br/>
      /// as the <c>IExactlyOnceTeventDeliveryLeg</c> set the <c>IUnitOfWorkTeventPublisher</c> routes through.</summary>
      internal class Factory
      {
         readonly Outbox.ITessageStorage _tessageStorage;

         internal Factory(Outbox.ITessageStorage tessageStorage) => _tessageStorage = tessageStorage;

         internal ExactlyOnceDeliveryStream CreateFor(TessagingConnection connection) => new(connection, _tessageStorage);
      }

      readonly TessagingConnection _connection;
      readonly Outbox.ITessageStorage _tessageStorage;
      //Keyed by delivery stream sequence number: the send loop always leads with the lowest-sequenced undelivered tessage,
      //and a second offer of an already queued sequence number collapses into the existing entry.
      readonly IThreadShared<SortedDictionary<long, TransportTessage.OutGoing>> _queue = IThreadShared.New(new SortedDictionary<long, TransportTessage.OutGoing>());
      readonly AutoResetEvent _signal = new(false);
      Thread? _sendLoopThread;
      int _consecutiveFailures;

      internal ExactlyOnceDeliveryStream(TessagingConnection connection, Outbox.ITessageStorage tessageStorage)
      {
         _connection = connection;
         _tessageStorage = tessageStorage;
      }

      internal void Enqueue(TransportTessage.OutGoing transportTessage)
      {
         var sequenceNumber = transportTessage.DeliveryStreamSequenceNumber._assert().NotNull().Value;
         _queue.Locked(queue =>
         {
            if(queue.TryGetValue(sequenceNumber, out var alreadyQueued))
            {
               //One sequence number is one tessage - the outbox save assigned it exactly once - so a second offer can only be
               //the same tessage arriving through the other enqueue path (commit hook vs recovery backlog load).
               State.Assert(alreadyQueued.TessageId == transportTessage.TessageId);
               return;
            }
            queue.Add(sequenceNumber, transportTessage);
         });
         _signal.Set();
      }

      internal void Start()
      {
         LoadUndeliveredTessages();
         _sendLoopThread = _connection._taskRunner.RunOnNamedThread($"ExactlyOnceDelivery-{_connection.EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);
      }

      internal void AwaitSendLoopTermination() => _sendLoopThread?.JoinCE(5.Seconds());

      //In-order delivery survives recovery: each backlog tessage re-enters the queue at its delivery stream sequence number,
      //so the send loop leads with the oldest undelivered tessage again - the sequence-keyed queue makes the enqueue order,
      //including a commit hook racing this load, irrelevant.
      void LoadUndeliveredTessages()
      {
         //The stream is a dedicated-thread head-of-line pump, deliberately synchronous - it bridges its storage's async calls
         //the same way it already bridges the transport post below.
         var undelivered = _tessageStorage.GetUndeliveredTessagesForEndpointAsync(_connection.EndpointInformation.Id).GetAwaiter().GetResult();
         if(undelivered.Count == 0) return;

         this.Log().Info($"Loading {undelivered.Count} undelivered tessage(s) for recovery to endpoint {_connection.EndpointInformation.Id}");
         foreach(var undeliveredTessage in undelivered)
         {
            var tessageType = undeliveredTessage.TypeId.Type;
            var tessage = _connection._serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);
            _connection.EnqueueForExactlyOnceDelivery(tessage, undeliveredTessage.TessageId, undeliveredTessage.DeliveryStreamSequenceNumber);
         }
      }

      void SendLoop()
      {
         this.Log().Info($"Started exactly-once delivery loop for endpoint {_connection.EndpointInformation.Id}");

         try
         {
            while(!_connection._cancellationSource.IsCancellationRequested)
            {
               //The lowest-sequenced undelivered tessage: head-of-line in the pair's delivery stream order.
               var pending = _queue.Locked(queue => queue.Count > 0 ? queue.First().Value : null);

               if(pending == null)
               {
                  WaitHandle.WaitAny([_signal, _connection._cancellationSource.Token.WaitHandle]);
                  continue;
               }

               if(TrySend(pending))
               {
                  _queue.Locked(queue => queue.Remove(pending.DeliveryStreamSequenceNumber._assert().NotNull().Value));

                  _consecutiveFailures = 0;
               } else
               {
                  _consecutiveFailures++;
                  var backoff = TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(_consecutiveFailures - 1, 7)));
                  this.Log().Debug($"Backing off exactly-once delivery to endpoint {_connection.EndpointInformation.Id} for {backoff.TotalSeconds:F1}s (consecutive failures: {_consecutiveFailures})");
                  WaitHandle.WaitAny([_signal, _connection._cancellationSource.Token.WaitHandle], backoff);
               }
            }
         }
         catch(OperationCanceledException) {} // Expected during shutdown: cancellation aborted an in-flight send.
         catch(ObjectDisposedException) {} // Expected during shutdown

         this.Log().Info($"Stopped exactly-once delivery loop for endpoint {_connection.EndpointInformation.Id}");
      }

      bool TrySend(TransportTessage.OutGoing pending)
      {
         try
         {
            //Freshly computed per attempt: sender-side stranding can punch a hole below the pending tessage
            //between attempts, and a refused attempt heals exactly by redeclaring the predecessor the durable rows now name.
            var predecessorSequenceNumber = _tessageStorage.GetDeliveryStreamPredecessorSequenceNumberAsync(_connection.EndpointInformation.Id, pending.DeliveryStreamSequenceNumber._assert().NotNull().Value).GetAwaiter().GetResult();
            _connection._transportMessagePoster.PostAsync(pending, _connection.RemoteAddress, predecessorSequenceNumber, _connection._cancellationSource.Token).GetAwaiter().GetResult();

            this.Log().Debug($"Delivered tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
            _connection._exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.MarkAsReceivedAsync(pending.TessageId, _connection.EndpointInformation.Id).GetAwaiter().GetResult());
            return true;
         }
#pragma warning disable CA1031 // Background thread — must catch all to keep the send loop running
         catch(Exception exception)
         {
#pragma warning restore CA1031
            //Shutdown cancelled the in-flight send: not a delivery failure. The tessage stays undelivered in the outbox and
            //redelivers on the next start (deduplicated by the receiver), so let the cancellation propagate and end the loop.
            _connection._cancellationSource.Token.ThrowIfCancellationRequested();
            this.Log().Warning(exception, $"Delivery failed for tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
            _connection._exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.RecordDeliveryFailureAsync(pending.TessageId, _connection.EndpointInformation.Id, exception).GetAwaiter().GetResult());
            return false;
         }
      }

      public void Dispose() => _signal.Dispose();
   }
}
