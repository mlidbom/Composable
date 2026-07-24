using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging._private.HandlerAvailability;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Peers._private;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging.TessageBus._private.Outbox;

static class OutboxRegistrar
{
   public static IComponentRegistrar Outbox(this IComponentRegistrar registrar)
      => registrar.Register(_private.Outbox.Outbox.RegisterWith);
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
partial class Outbox : IOutbox
{
   internal static void RegisterWith(IComponentRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, IPeerRegistry peerRegistry, IHandlerAvailability handlerAvailability)
                                                => new Outbox(tessagingRouter, tessageStorage, peerRegistry, handlerAvailability)));
      //Wiring the outbox is what wires the endpoint's exactly-once tevent delivery: the outbox joins the delivery-leg set the IUnitOfWorkTeventPublisher routes through...
      registrar.Register(Singleton.ForSet<IExactlyOnceTeventDeliveryLeg>().CreatedBy((IOutbox outbox) => outbox));
      //...and what grants the router's connections their exactly-once delivery streams, backed by the outbox's storage: on an endpoint without the outbox this set is empty and connections carry no such stream (see TessagingConnection).
      registrar.Register(Singleton.ForSet<TessagingConnection.ExactlyOnceDeliveryStream.Factory>().CreatedBy((ITessageStorage tessageStorage) => new TessagingConnection.ExactlyOnceDeliveryStream.Factory(tessageStorage)));
      //...and the outbox's side of the peer lifecycle: the peer registry notifies the observer set on every recorded advertisement, and this member keeps the undelivered rows consistent with what each peer's advertisement declares. An endpoint without the outbox contributes nothing.
      registrar.Register(Singleton.ForSet<IPeerLifecycleObserver>().CreatedBy((ITessageStorage tessageStorage) => new PeerLifecycleObserver(tessageStorage)));
      registrar.Register(TessageStorage.RegisterWith);
   }

   readonly ITessageStorage _storage;
   readonly IPeerRegistry _peerRegistry;
   readonly ITessagingRouter _tessagingRouter;
   readonly IHandlerAvailability _handlerAvailability;

   Outbox(ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, IPeerRegistry peerRegistry, IHandlerAvailability handlerAvailability)
   {
      _storage = tessageStorage;
      _peerRegistry = peerRegistry;
      _tessagingRouter = tessagingRouter;
      _handlerAvailability = handlerAvailability;
   }

   public async Task PublishTransactionallyAsync(IPublisherTevent<IExactlyOnceTevent> wrappedTevent)
   {
      State.NotNull(Transaction.Current);
      //Captured before the first await: the commit hook must bind to the caller's unit of work even if the ambient has drifted in a continuation.
      var transaction = Transaction.Current;
      var dedupId = wrappedTevent.Tevent.Id;
      this.Log().Debug($"Will publish tevent if transaction succeeds: {dedupId} ({wrappedTevent.GetType().Name})");

      //Fan-out membership is the peer registry's remembered subscribers, never the live connections: a subscribing peer that is
      //down at publish time still gets its receiver row, and the recovery backlog its next connection loads delivers the tevent
      //on its return. (A peer is another endpoint, so the registry never lists us: tevents to ourselves dispatch synchronously in-process.)
      var subscriberIds = _peerRegistry.SubscriberIdsFor(wrappedTevent);
      var assignedSequenceNumbers = await _storage.SaveTessageAsync(wrappedTevent, dedupId, [..subscriberIds]).caf();

      transaction.OnCommittedSuccessfully(() =>
      {
         //Delivery starts now toward the listed peers' live connections, looked up at commit rather than at publish: a
         //subscriber connection that appeared in between loaded its recovery backlog before this row committed, so only a
         //commit-time lookup sees it. Intersecting keeps enqueue ⊆ persist (the registry is written before routes are built
         //from an advertisement — see TessagingRouter.ConnectAsync); a listed peer with no live connection here is exactly
         //what the backlog load covers when its next connection's delivery starts - and should both paths offer the tessage,
         //the stream's sequence-keyed queue collapses them.
         _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                         .Where(connection => subscriberIds.Contains(connection.EndpointInformation.Id))
                         .ForEach(connection =>
                          {
                             this.Log().Debug($"OnCommittedSuccessfully: Delivering tevent {dedupId} to endpoint {connection.EndpointInformation.Id}");
                             connection.EnqueueForExactlyOnceDelivery(wrappedTevent, dedupId, assignedSequenceNumbers[connection.EndpointInformation.Id]);
                          });
      });
   }

   public async Task SendTransactionallyAsync(IExactlyOnceTommand exactlyOnceTommand)
   {
      State.NotNull(Transaction.Current);
      //Captured before the first await: the commit hook must bind to the caller's unit of work even if the ambient has drifted in a continuation.
      var transaction = Transaction.Current;
      this.Log().Debug($"Will send tommand if transaction succeeds: {exactlyOnceTommand.Id} ({exactlyOnceTommand.GetType().Name})");

      //The tommand binds to its one specific receiver here, at send: every tessage between a sender and a receiver rides that
      //pair's single ordered, receiver-deduped delivery stream, which is what makes exactly-once in-order hold by construction.
      //(Routing at delivery time was tried and retracted: re-delivery could reach an endpoint whose inbox never saw the
      //tommand, breaking exactly-once across handler replacement - see src/Compze.Tessaging/dev_docs/DONE/durable-peer-topology.md.)
      //The bind is a waiting send: with no bindable receiver right now - never-seen, or several remembered with none live -
      //it waits, within the endpoint's handler-availability patience, inside the caller's unit of work. Safe to wait here even
      //on SQLite, where the caller's open transaction holds the domain database's write gate: the recording that satisfies this
      //wait (RecordAdvertisementAsync) does not need that gate - its reads run unenlisted, and on sqlite the advertisement save
      //commits to the peer registry's own database behind its own gate (see the sqlite backend's
      //ISqlitePeerRegistryConnectionPool). Only stranding what a shrink renounced writes the domain database, so the one
      //recording that can queue behind this caller's open transaction is a replacement that both adds the awaited type and
      //renounces another - it lands when the blocking transaction completes, worst case this wait's own patience.
      var receiverId = await _handlerAvailability.AwaitBindableReceiverOfAsync(exactlyOnceTommand.GetType()).caf();
      var assignedSequenceNumbers = await _storage.SaveTessageAsync(exactlyOnceTommand, exactlyOnceTommand.Id, receiverId).caf();

      transaction.OnCommittedSuccessfully(() =>
      {
         //Looked up at commit rather than at send: a connection that appeared in between loaded its recovery backlog before
         //this row committed, so only a commit-time lookup sees it. The row is bound: it must enter no other endpoint's
         //stream, so a live handler that is not the bound receiver leaves the row waiting for its endpoint's return.
         var liveConnection = _tessagingRouter.LiveConnectionToHandlerFor(exactlyOnceTommand.GetType());
         if(liveConnection == null || !liveConnection.EndpointInformation.Id.Equals(receiverId)) return;
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tommand {exactlyOnceTommand.Id} to endpoint {receiverId}");
         liveConnection.EnqueueForExactlyOnceDelivery(exactlyOnceTommand, exactlyOnceTommand.Id, assignedSequenceNumbers[receiverId]);
      });
   }

   bool _running = false;

   //The router's delivery lifecycle is not the outbox's to drive: it belongs to the distributed Tessaging core's component
   //(DistributedTessagingEndpointComponent), which starts delivery only after every endpoint's listening phase — by which time
   //this storage is initialized, so each connection's exactly-once stream can load its recovery backlog.
   public async Task StartAsync()
   {
      State.Assert(!_running);
      this.Log().Info("Starting");

      await _storage.StartAsync().caf();

      _running = true;
      this.Log().Info("Started");
   }

   public async Task StopAsync()
   {
      State.Assert(_running);
      _running = false;
      this.Log().Info("Stopped");
      await Task.CompletedTask.caf();
   }
}
