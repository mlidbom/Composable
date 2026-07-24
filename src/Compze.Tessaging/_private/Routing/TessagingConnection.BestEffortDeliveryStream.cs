using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus._private.BestEffortDelivery;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging._private.Routing;

partial class TessagingConnection
{
   ///<summary>The connection's best-effort delivery stream: the pump that drains the peer's in-memory queue<br/>
   /// (<see cref="BestEffortTeventQueues.PeerQueue"/>, which outlives the connection) in publish order while the connection is<br/>
   /// live. A delivery failure pauses the stream whole: the one tessage in flight at the failure is dropped, loudly — without<br/>
   /// receiver dedup a re-send could duplicate it — while everything queued behind it stays queued in order, and draining resumes<br/>
   /// once the peer answers a tessage-free probe (or a new connection to the returned peer drains the same queue). The tier's<br/>
   /// loss surface is exactly: that single in-flight tessage, a crash of this process (memory is memory), and queue overflow<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>).</summary>
   internal class BestEffortDeliveryStream
   {
      ///<summary>Creates the <see cref="BestEffortDeliveryStream"/> each connection carries, binding it to the peer's queue in<br/>
      /// <see cref="BestEffortTeventQueues"/> — registered by the best-effort delivery wiring, which owns the queues: the same<br/>
      /// wiring-supplies-the-streams idiom as the exactly-once stream factory the outbox registers.</summary>
      internal class Factory
      {
         readonly BestEffortTeventQueues _queues;

         internal Factory(BestEffortTeventQueues queues) => _queues = queues;

         internal BestEffortDeliveryStream CreateFor(TessagingConnection connection) => new(connection, _queues);
      }

      readonly TessagingConnection _connection;
      readonly BestEffortTeventQueues _queues;
      BestEffortTeventQueues.PeerQueue _peerQueue = null!;
      Thread? _sendLoopThread;
      int _consecutiveProbeFailures;

      BestEffortDeliveryStream(TessagingConnection connection, BestEffortTeventQueues queues)
      {
         _connection = connection;
         _queues = queues;
      }

      internal void Start()
      {
         //Resolved here rather than at construction: the peer's identity arrives with the connection's InitAsync. A required
         //peer met for the first time has its held tevents resolved against its just-learned subscriptions before draining
         //starts. The router recorded the advertisement in the peer registry before this runs (ConnectAsync), which is the
         //ordering the delivery leg's queues-before-registry read relies on.
         _peerQueue = _queues.ForConnectedPeer(_connection.EndpointInformation);

         //Attached here, synchronously, before the loop's thread exists - the router adds this connection and starts its
         //delivery under one hold of its monitor, so by the time anything can route a publish to the peer its queue accepts.
         //Attaching from inside the loop instead made the queue start accepting only once a freshly spawned BelowNormal thread
         //got scheduled, and a not-queued-for peer's tevents published in that window were dropped with the connection live.
         _peerQueue.AttachDrainingStream();
         _sendLoopThread = _connection._taskRunner.RunOnNamedThread($"BestEffortDelivery-{_connection.EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);
      }

      ///<summary>This connection stops being the peer's delivery path — called as delivery stops, so the queue stops accepting<br/>
      /// the moment the connection stops being live rather than whenever the send loop's thread gets round to noticing.</summary>
      ///<remarks>For a not-queued-for peer this also empties the queue: such a peer's tevents exist only while it is there to<br/>
      /// receive them, so whatever the stopping stream never got to send must not survive to be delivered on the peer's return.<br/>
      /// The same drop-stream-whole boundary a failed delivery produces (<see cref="DropTheQueuedStreamWholeForTheNotQueuedForPeer"/>),<br/>
      /// reached by the connection ending instead of by a send failing.</remarks>
      internal void DetachFromThePeersQueue()
      {
         _peerQueue.DetachDrainingStream();
         if(!_peerQueue.QueueingDeclined) return;

         var neverSent = _peerQueue.DequeueAll();
         if(neverSent.Count == 0) return;

         this.Log().Info($"Delivery to not-queued-for endpoint {_connection.EndpointInformation.Id} stopped with {neverSent.Count} tessage(s) still queued: they are dropped rather than kept for the peer's return - the composition declared this endpoint keeps nothing for it.");
         foreach(var dropped in neverSent)
            _connection._tessagesInFlightTracker.DroppedBeforeDelivery(dropped, _connection.EndpointInformation.Id);
      }

      internal void AwaitSendLoopTermination() => _sendLoopThread?.JoinCE(5.Seconds());

      void SendLoop()
      {
         this.Log().Info($"Started best-effort delivery loop for endpoint {_connection.EndpointInformation.Id}");

         try
         {
            while(!_connection._cancellationSource.IsCancellationRequested)
            {
               //Dequeued before sending, never peeked: nothing on this tier is ever re-sent - there is no receiver dedup to
               //make a re-send safe - so a tessage whose delivery attempt fails is gone rather than risked twice.
               var pending = _peerQueue.TryDequeue();

               if(pending == null)
               {
                  WaitHandle.WaitAny([_peerQueue.EnqueuedSignal, _connection._cancellationSource.Token.WaitHandle]);
                  continue;
               }

               try
               {
                  _connection._transportMessagePoster.PostAsync(pending, _connection.RemoteAddress, cancellationToken: _connection._cancellationSource.Token).GetAwaiter().GetResult();
                  this.Log().Debug($"Delivered best-effort tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
               }
#pragma warning disable CA1031 // Background thread, and the failure policies below ARE the best-effort tier's handling of every delivery failure.
               catch(Exception exception)
               {
#pragma warning restore CA1031
                  //Shutdown cancelled the in-flight send: exit the loop rather than treating it as a delivery failure to drop or pause on.
                  _connection._cancellationSource.Token.ThrowIfCancellationRequested();
                  if(_peerQueue.QueueingDeclined)
                     DropTheQueuedStreamWholeForTheNotQueuedForPeer(failedTessage: pending, exception);
                  else
                     DropTheInFlightTessageAndPauseUntilThePeerAnswersAProbe(inFlightAtFailure: pending, exception);
               }
            }
         }
         catch(OperationCanceledException) {} // Expected during shutdown: cancellation aborted an in-flight send or probe.
         catch(ObjectDisposedException) {} // Expected during shutdown

         this.Log().Info($"Stopped best-effort delivery loop for endpoint {_connection.EndpointInformation.Id}");
      }

      ///<summary>The failure policy of a peer the composition declared not-queued-for (<c>DoNotQueueTeventsFor</c>): the failed<br/>
      /// tessage and everything queued behind it are dropped together, so the subscriber's gap is one clean boundary — never a<br/>
      /// silent mid-stream skip — and the loop keeps attempting whatever is published afterwards. No pause, no probe: the<br/>
      /// composition declared it does not care whether this peer is up.</summary>
      void DropTheQueuedStreamWholeForTheNotQueuedForPeer(TransportTessage.OutGoing failedTessage, Exception exception)
      {
         var droppedBehindFailed = _peerQueue.DequeueAll();
         this.Log().Warning(exception, $"Best-effort delivery to not-queued-for endpoint {_connection.EndpointInformation.Id} failed: dropping the queued stream whole - the failed tessage {failedTessage.TessageId} plus {droppedBehindFailed.Count} tessage(s) queued behind it. The subscriber resumes from tessages published after this point.");

         _connection._tessagesInFlightTracker.DroppedBeforeDelivery(failedTessage, _connection.EndpointInformation.Id);
         foreach(var dropped in droppedBehindFailed)
            _connection._tessagesInFlightTracker.DroppedBeforeDelivery(dropped, _connection.EndpointInformation.Id);
      }

      ///<summary>Pause-stream-whole: the failed tessage was in flight at the failure moment — the one intrinsically ambiguous<br/>
      /// loss, dropped loudly — and the remainder stays queued in order while delivery pauses. Draining resumes only when the<br/>
      /// peer answers the endpoint-information query: a tessage-free probe, so waiting out an outage never spends real tessages<br/>
      /// finding out whether the peer is back. A peer that returns at a new address resumes through the router instead — the<br/>
      /// replacement connection's stream drains this same queue, and this paused loop dies with its dropped connection.</summary>
      void DropTheInFlightTessageAndPauseUntilThePeerAnswersAProbe(TransportTessage.OutGoing inFlightAtFailure, Exception exception)
      {
         this.Log().Warning(exception, $"Best-effort delivery to endpoint {_connection.EndpointInformation.Id} failed: the in-flight tessage {inFlightAtFailure.TessageId} is dropped - without receiver dedup a re-send could duplicate it - and the stream pauses with the remainder queued, resuming when the peer is reachable again.");
         _connection._tessagesInFlightTracker.DroppedBeforeDelivery(inFlightAtFailure, _connection.EndpointInformation.Id);

         _consecutiveProbeFailures = 0;
         while(!_connection._cancellationSource.IsCancellationRequested)
         {
            var backoff = TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(_consecutiveProbeFailures, 7)));
            WaitHandle.WaitAny([_connection._cancellationSource.Token.WaitHandle], backoff);
            if(_connection._cancellationSource.IsCancellationRequested) return;

            try
            {
               _connection._endpointDiscoveryQueryTransport.GetAsync(new EndpointInformationQuery(), _connection.RemoteAddress, _connection._cancellationSource.Token).GetAwaiter().GetResult();
               this.Log().Info($"Endpoint {_connection.EndpointInformation.Id} answered the probe; best-effort delivery resumes with the queued remainder.");
               return;
            }
#pragma warning disable CA1031 // The probe failing means the peer is still unreachable - exactly what is being waited out.
            catch(Exception probeException)
            {
#pragma warning restore CA1031
               //Shutdown cancelled the probe: exit rather than counting it as the peer still being unreachable.
               _connection._cancellationSource.Token.ThrowIfCancellationRequested();
               _consecutiveProbeFailures++;
               this.Log().Debug($"Probe of endpoint {_connection.EndpointInformation.Id} failed ({probeException.GetType().Name}); best-effort delivery stays paused (consecutive probe failures: {_consecutiveProbeFailures}).");
            }
         }
      }
   }
}
