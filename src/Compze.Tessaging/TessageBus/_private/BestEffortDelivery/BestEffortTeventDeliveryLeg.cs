using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.Peers._private;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.BestEffortDelivery;

static class BestEffortTeventDeliveryRegistrar
{
   ///<summary><paramref name="requiredPeers"/> and <paramref name="peersNotQueuedFor"/> are captured by reference and read when<br/>
   /// the container builds — after the composition's <c>RequirePeers</c>/<c>DoNotQueueTeventsFor</c> declarations, which the<br/>
   /// distributed Tessaging feature collects into them.</summary>
   public static IComponentRegistrar BestEffortTeventDelivery(this IComponentRegistrar registrar, IReadOnlyList<EndpointId> requiredPeers, IReadOnlyList<EndpointId> peersNotQueuedFor)
      => registrar.Register(Singleton.For<BestEffortTeventQueues>()
                                     .CreatedBy((ITypeMap typeMap, ITessagesInFlightTracker tessagesInFlightTracker) => new BestEffortTeventQueues(requiredPeers, peersNotQueuedFor, typeMap, tessagesInFlightTracker)))
                  //The stream factory grants the router's connections their best-effort delivery streams, each draining its peer's queue - the same wiring idiom as the exactly-once stream factory the outbox registers.
                  .Register(Singleton.For<TessagingConnection.BestEffortDeliveryStream.Factory>()
                                     .CreatedBy((BestEffortTeventQueues queues) => new TessagingConnection.BestEffortDeliveryStream.Factory(queues)))
                  .Register(BestEffortTeventDeliveryLeg.RegisterWith);
}

///<summary>The <see cref="IBestEffortTeventDeliveryLeg"/>: fans a published best-effort tevent out to the peer registry's<br/>
/// remembered subscribers — never to whoever happens to be connected — by enqueueing it, on commit, into each subscriber's<br/>
/// in-memory queue (<see cref="BestEffortTeventQueues"/>). A live subscriber's connection drains its queue immediately; a<br/>
/// subscriber that is down accumulates its tessages in order and receives them on its return<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c> and <c>src/Compze.Tessaging/dev_docs/peers.md</c>).</summary>
class BestEffortTeventDeliveryLeg : IBestEffortTeventDeliveryLeg
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      //Wiring the leg into the delivery-leg set is what makes the endpoint's IUnitOfWorkTeventPublisher route best-effort tevents across the wire.
      => registrar.Register(Singleton.ForSet<IBestEffortTeventDeliveryLeg>()
                                     .CreatedBy((IPeerRegistry peerRegistry, BestEffortTeventQueues queues, ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, ITessagingSerializer serializer)
                                                   => new BestEffortTeventDeliveryLeg(peerRegistry, queues, tessagesInFlightTracker, typeMap, serializer)));

   readonly IPeerRegistry _peerRegistry;
   readonly BestEffortTeventQueues _queues;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMap _typeMap;
   readonly ITessagingSerializer _serializer;

   BestEffortTeventDeliveryLeg(IPeerRegistry peerRegistry, BestEffortTeventQueues queues, ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, ITessagingSerializer serializer)
   {
      _peerRegistry = peerRegistry;
      _queues = queues;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMap = typeMap;
      _serializer = serializer;
   }

   public void PublishBestEffort(IPublisherTevent<IRemotableTevent> wrappedTevent)
   {
      //Fan-out membership is the peer registry's remembered subscribers, never the live connections: a subscribing peer that is
      //down at publish time still gets the tevent queued, and its next connection's stream drains the queue on its return.
      //(A peer is another endpoint, so the registry never lists us: tevents to ourselves dispatch synchronously in-process.)
      //A required peer not yet met additionally gets everything - its subscriptions are unknown until its first advertisement,
      //which resolves what was held for it. Read from the queues BEFORE the registry, deliberately: first contact records the
      //advertisement in the registry before the held tevents flush, so this read order can never miss a peer mid-transition -
      //the reverse order could see it in neither.
      var peersAwaitingFirstContact = _queues.PeersAwaitingFirstContact;
      var subscriberIds = peersAwaitingFirstContact.Count == 0
                             ? _peerRegistry.SubscriberIdsFor(wrappedTevent)
                             : [..peersAwaitingFirstContact.Union(_peerRegistry.SubscriberIdsFor(wrappedTevent))];
      if(subscriberIds.Count == 0) return;

      //One envelope identity per publish, shared by every subscriber's delivery: it carries no dedup meaning on this leg
      //(nothing is ever re-sent) and exists so in-flight tracking sees one tessage fanning out to many endpoints.
      //Serialized here, at publish: a tevent that cannot be serialized fails the publish inside the caller's transaction.
      var envelopeId = new TessageId();
      var transportTessage = TransportTessage.OutGoing.Create(wrappedTevent, envelopeId, _typeMap, _serializer);
      this.Log().Debug($"Publishing best-effort tevent {envelopeId} ({wrappedTevent.GetType().Name}) to {subscriberIds.Count} subscriber peer(s)");

      //Queue slots are reserved here, at publish, inside the caller's transaction, so a full queue fails the publish loud
      //(BestEffortTeventQueueOverflowException) - backpressure, never post-commit shedding. Commit converts the reservations
      //into queued tevents; a transaction completing any other way releases them.
      var subscribers = ReserveASlotOnEverySubscribersQueue(subscriberIds);

      //The publisher asserts the ambient transaction before routing any delivery leg, so a best-effort tevent is always published from within a unit of work: remote delivery happens on commit, never immediately.
      var transaction = Transaction.Current._assert().NotNull();
      transaction.OnCommittedSuccessfully(EnqueueOnEverySubscribersQueue);
      transaction.OnCompletedWithoutCommitting(() => subscribers.ForEach(subscriber => subscriber.Queue.ReleaseReservation()));
      return;

      void EnqueueOnEverySubscribersQueue()
      {
         foreach(var subscriber in subscribers)
         {
            _tessagesInFlightTracker.SendingTessageOnTransport(transportTessage, subscriber.Id);
            //A not-queued-for peer's queue declines the tessage while no stream is draining it: published while such a peer is down, the tevent is simply gone - the declared opt-down.
            if(!subscriber.Queue.Enqueue(transportTessage))
               _tessagesInFlightTracker.DroppedBeforeDelivery(transportTessage, subscriber.Id);
         }
      }
   }

   List<(EndpointId Id, BestEffortTeventQueues.PeerQueue Queue)> ReserveASlotOnEverySubscribersQueue(IReadOnlyList<EndpointId> subscriberIds)
   {
      var reserved = new List<(EndpointId Id, BestEffortTeventQueues.PeerQueue Queue)>(subscriberIds.Count);
      try
      {
         foreach(var subscriberId in subscriberIds)
         {
            var queue = _queues.For(subscriberId);
            queue.ReserveSlot();
            reserved.Add((subscriberId, queue));
         }
         return reserved;
      }
      catch //Resource cleanup, not handling: the failing publish must not leak the reservations it already took from the other subscribers' queues.
      {
         reserved.ForEach(subscriber => subscriber.Queue.ReleaseReservation());
         throw;
      }
   }
}
