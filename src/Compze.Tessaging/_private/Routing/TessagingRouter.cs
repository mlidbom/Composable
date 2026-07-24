using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;
using Compze.Threading._internal;
using Compze.TypeIdentifiers;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging._private.Routing;

static class TessagingTransportRegistrar
{
   internal static IComponentRegistrar TessagingTransport(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRouter.RegisterWith);
}

class TessagingRouter : ITessagingRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingRouter>().CreatedBy(
            (ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, ITessagingSerializer serializer, ITransportMessagePoster transportMessagePoster, IEndpointInformationQueryTransport endpointDiscoveryQueryTransport, TessagingConnection.BestEffortDeliveryStream.Factory bestEffortStreamFactory, IComponentSet<TessagingConnection.ExactlyOnceDeliveryStream.Factory> exactlyOnceStreamFactory, IPeerRegistry peerRegistry, EndpointConfiguration configuration, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
               => new TessagingRouter(tessagesInFlightTracker, typeMap, serializer, transportMessagePoster, endpointDiscoveryQueryTransport, bestEffortStreamFactory, exactlyOnceStreamFactory, peerRegistry, configuration, taskRunner, exceptionReporter)));

   ///<summary>How long a reconciliation pass waits for a registry change signal before running anyway. The signal makes announced<br/>
   /// and retracted endpoints propagate at signal latency; this periodic pass exists for what no signal can carry — a crashed<br/>
   /// process's addresses disappearing (a crash signals nothing) and retrying connections that failed.</summary>
   static readonly TimeSpan ReconcileLivenessInterval = TimeSpan.FromSeconds(1);

   //Awaitable: releasing an update lock wakes every condition wait (TryAwaitConnectionsOrPeerMemorySatisfyingAsync), so state
   //mutations take Update and lookups take Read - on this monitor both are exclusive, only the wake-on-release differs.
   readonly IAwaitableMonitor _monitor = IAwaitableMonitor.New();
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMap _typeMap;
   readonly ITessagingSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly IEndpointInformationQueryTransport _endpointDiscoveryQueryTransport;
   readonly TessagingConnection.BestEffortDeliveryStream.Factory _bestEffortStreamFactory;
   //Null when the endpoint wires no outbox — the guarantee-free distributed composition: its connections then carry no exactly-once delivery stream.
   readonly TessagingConnection.ExactlyOnceDeliveryStream.Factory? _exactlyOnceStreamFactory;
   readonly IPeerRegistry _peerRegistry;
   readonly EndpointConfiguration _configuration;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   readonly CancellationTokenSource _reconcileLoopCancellation = new();
   Task? _reconcileLoop;
   IEndpointRegistry? _endpointRegistry;
   EndpointAddress? _ownAddress;

   bool _deliveryStarted;
   bool _stopped;
   bool _disposed;

   readonly Dictionary<EndpointId, TessagingConnection> _connections = new();
   readonly Dictionary<Type, TessagingConnection> _tommandHandlerRoutes = new();
   //Multi-entry, deliberately: several live endpoints advertising one typermedia type is a diagnosable send-time condition
   //(MultipleHandlersForTypermediaTypeException), never a rebuild failure and never a silent pick.
   readonly Dictionary<Type, List<TessagingConnection>> _typermediaHandlerRoutes = new();
   readonly List<(Type TeventType, TessagingConnection Connection)> _teventSubscriberRoutes = [];
   readonly Dictionary<Type, IReadOnlyList<TessagingConnection>> _teventSubscriberRouteCache = new();

   TessagingRouter(ITessagesInFlightTracker tessagesInFlightTracker, ITypeMap typeMap, ITessagingSerializer serializer, ITransportMessagePoster transportMessagePoster, IEndpointInformationQueryTransport endpointDiscoveryQueryTransport, TessagingConnection.BestEffortDeliveryStream.Factory bestEffortStreamFactory, IEnumerable<TessagingConnection.ExactlyOnceDeliveryStream.Factory> exactlyOnceStreamFactory, IPeerRegistry peerRegistry, EndpointConfiguration configuration, ITaskRunner taskRunner, IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMap = typeMap;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _endpointDiscoveryQueryTransport = endpointDiscoveryQueryTransport;
      _bestEffortStreamFactory = bestEffortStreamFactory;
      _exactlyOnceStreamFactory = exactlyOnceStreamFactory.SingleOrDefault();
      _peerRegistry = peerRegistry;
      _configuration = configuration;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public async Task StartMaintainingConnectionsAsync(IEndpointRegistry? endpointRegistry, EndpointAddress ownAddress)
   {
      _endpointRegistry = endpointRegistry;
      _ownAddress = ownAddress;
      await ReconcileConnectionsAsync().caf();
      //With no registry declared there is no membership to converge on - the endpoint connects to no other endpoint - so there is nothing to keep reconciling.
      if(endpointRegistry is not null)
         _reconcileLoop = TaskCE.Run(ReconcileLoopAsync);
   }

   ///<summary>Waits until the registry signals that membership may have changed — or <see cref="ReconcileLivenessInterval"/><br/>
   /// elapses, covering what no signal carries — then reconciles. An async loop, not a thread blocked on the reconcile.</summary>
   ///<remarks>The reconcile pass runs as an awaited continuation, never a thread blocked on it. A pass records peer advertisements<br/>
   /// (<see cref="IPeerRegistry"/>) inside a <see cref="System.Transactions.TransactionScope"/>; blocking a thread on that async<br/>
   /// work — the previous <c>WaitUnwrappingException</c> — stranded the ambient transaction, so its connection held its locks until<br/>
   /// the transaction timeout. Only the registry wait blocks, offloaded off the loop so it never blocks the pass's continuations.</remarks>
   async Task ReconcileLoopAsync()
   {
      while(!_reconcileLoopCancellation.IsCancellationRequested)
      {
         await TaskCE.Run(() => _endpointRegistry!.AwaitPossibleMembershipChange(ReconcileLivenessInterval, _reconcileLoopCancellation.Token)).caf();
         if(_reconcileLoopCancellation.IsCancellationRequested) return;

         try
         {
            await ReconcileConnectionsAsync().caf();
         }
#pragma warning disable CA1031 //A reconciliation pass must never kill the loop; an unexpected failure is reported and surfaces the way every background exception does.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            _exceptionReporter.ReportException(exception);
         }
      }
   }

   ///<summary>One reconciliation pass: connections converge on the registry's current membership — minus our own announced<br/>
   /// address, which the registry lists like any other. Routes lead only to <em>other</em> endpoints: nothing self-addressed ever<br/>
   /// crosses the wire, because the roster serves in-roster tommands inline and the endpoint's own tevent subscriptions by<br/>
   /// in-boundary participation (the consistency law), so the router maintains no connection to self.</summary>
   async Task ReconcileConnectionsAsync()
   {
      var desiredAddresses = (_endpointRegistry?.ServerEndpointAddresses ?? []).ToHashSet();
      desiredAddresses.Remove(_ownAddress!);

      var (addressesToConnect, connectionsToDrop) = _monitor.Read<(List<EndpointAddress>, List<TessagingConnection>)>(() =>
      {
         if(_stopped) return ([], []);
         var connectedAddresses = _connections.Values.Select(connection => connection.RemoteAddress).ToHashSet();
         return ([..desiredAddresses.Where(address => !connectedAddresses.Contains(address))],
                 [.._connections.Values.Where(connection => !desiredAddresses.Contains(connection.RemoteAddress))]);
      });

      //An endpoint whose address left the registry is gone (stopped, or crashed and pruned by liveness). Its undelivered
      //tessages stay in the outbox's storage - exactly-once means they wait for the endpoint's return, and the connection
      //created when it returns loads them, in send order.
      connectionsToDrop.ForEach(DropConnection);

      foreach(var address in addressesToConnect)
      {
         try
         {
            await ConnectAsync(address).caf();
         }
#pragma warning disable CA1031 //A listed address may have just crashed (the liveness filter has not pruned it yet) or not be reachable yet - topology churn, not a bug. The next pass retries; a persistent failure keeps being logged.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            this.Log().Warning(exception, $"Connecting to registered endpoint address {address.Uri} failed; will retry on the next reconciliation pass.");
         }
      }
   }

   void DropConnection(TessagingConnection connection) => _monitor.Update(() =>
   {
      _connections.Remove(connection.EndpointInformation.Id);
      RebuildRoutes();
      connection.Dispose();
   });

   async Task ConnectAsync(EndpointAddress remoteEndpointAddress)
   {
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new TessagingConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMap, _serializer, _transportMessagePoster, _endpointDiscoveryQueryTransport, _bestEffortStreamFactory, _exactlyOnceStreamFactory, _taskRunner, _exceptionReporter);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

      //Peer memory is recorded on every advertisement fetch: first contact creates the peer, a re-fetch replaces its stored
      //advertisement (see src/Compze.Tessaging/dev_docs/peers.md).
      try
      {
         //A peer is another endpoint, and our own address never enters a reconciliation pass - so an answer claiming our
         //identity means another process is running under this endpoint's EndpointId: a misconfiguration that must fail loud,
         //never be remembered as a peer.
         State.Assert(connection.EndpointInformation.Id != _configuration.Id,
                      () => $"The endpoint at {remoteEndpointAddress.Uri} answered discovery with this endpoint's own identity ({_configuration.Id}): another process is running under this endpoint's EndpointId, and an endpoint runs in exactly one process at a time.");
         await _peerRegistry.RecordAdvertisementAsync(connection.EndpointInformation).caf();
      }
      catch
      {
         //The reconciliation pass retries the whole connect: an unrecorded advertisement must not leave a live connection behind, or no later pass would ever record it.
         connection.Dispose();
         throw;
      }

      _monitor.Update(() =>
      {
         if(_stopped) //A reconciliation pass racing shutdown: the router is no longer connecting to anyone.
         {
            connection.Dispose();
            return;
         }

         var endpointId = connection.EndpointInformation.Id;
         if(_connections.TryGetValue(endpointId, out var existingConnection))
         {
            //The endpoint restarted at a new address (addresses are per-instance; identity is the EndpointId): replace the
            //connection. The new connection loads the endpoint's undelivered backlog when its delivery starts, so the
            //backlog follows the endpoint to its new address.
            existingConnection.Dispose();
            _connections[endpointId] = connection;
            RebuildRoutes();
         }
         else
         {
            _connections.Add(endpointId, connection);
            RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
         }

         //Load-bearing: registering the routes and loading the connection's recovery backlog (StartDelivery) happen under
         //one hold of this monitor, which route lookups also take. The outbox's commit-time lookups rely on exactly this: a
         //lookup that sees the connection can safely enqueue on it, and a row whose lookup missed the connection committed
         //before the backlog query ran - OnCommittedSuccessfully runs after commit - so the backlog is what delivers it.
         //Move the backlog load out of the monitor and tessages start silently slipping between the two.
         if(_deliveryStarted) connection.StartDelivery();
      });
   }

   public void StartDelivery() => _monitor.Update(() =>
   {
      _deliveryStarted = true;
      foreach(var connection in _connections.Values)
         connection.StartDelivery();
   });

   public void StopDelivery()
   {
      _reconcileLoopCancellation.Cancel();
      //Join the reconcile loop before returning: a pass in flight holds the peer-registry advertisement transaction open
      //(ReconcileConnectionsAsync -> ConnectAsync -> RecordAdvertisementAsync), so stopping delivery must mean that transaction
      //has reached its terminal state - not merely that the loop was signalled. Awaited outside the monitor: a pass in flight
      //takes the monitor to finish, so joining while holding it would deadlock. Ordered before the connections are stopped so
      //their delivery threads are no longer blocked behind that transaction's locks when they are joined.
      _reconcileLoop?.WaitUnwrappingException();
      _monitor.Update(() =>
      {
         _deliveryStarted = false;
         foreach(var connection in _connections.Values)
            connection.StopDelivery();
      });
   }

   ///<summary>Rebuilds the route tables from the current connections — membership changed, so routes derived from a dropped or replaced connection must not survive.</summary>
   void RebuildRoutes()
   {
      _tommandHandlerRoutes.Clear();
      _typermediaHandlerRoutes.Clear();
      _teventSubscriberRoutes.Clear();
      _teventSubscriberRouteCache.Clear();
      foreach(var connection in _connections.Values)
         RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
   }

   ///<summary>Builds a route for <em>every</em> type <paramref name="connection"/>'s endpoint advertises — an advertised type no<br/>
   /// route serves would be a silently dead subscription, so anything unroutable is asserted against instead of skipped.<br/>
   /// The advertising endpoint's <see cref="TessageHandlerRoster"/> asserts the same soundness when its advertisement is<br/>
   /// first computed, where a violation fails loudest.</summary>
   void RegisterRoutes(TessagingConnection connection, ISet<string> handledTypeIdStrings)
   {
      foreach(var typeIdString in handledTypeIdStrings)
      {
         var tessageType = _typeMap.GetId(typeIdString).Type;

         if(tessageType.Is<ITevent>())
         {
            //An advertised tevent subscription is a wrapper type; covariance answers whether the tevents it matches may travel remotely at all.
            //Which delivery leg a matching tevent travels is not routing's concern - the published tevent's own type decides that (see IUnitOfWorkTeventPublisher).
            State.Assert(tessageType.Is<IPublisherTevent<IRemotableTevent>>(),
                         () => $"Endpoint {connection.EndpointInformation.Id} advertises the tevent subscription {tessageType.FullName}, which no route can serve: an advertised tevent subscription is the wrapper type matching every wrapping of a remotable tevent type ({nameof(IPublisherTevent<>)}<{nameof(IRemotableTevent)}>). Every advertised type must get a route — a subscription must never be silently dropped.");
            _teventSubscriberRoutes.Add((tessageType, connection));
         } else if(tessageType.Is<IAtMostOnceTypermediaTommand>() || tessageType.Is<IRemotableTuery<object>>())
         {
            _typermediaHandlerRoutes.GetOrAdd(tessageType, () => []).Add(connection);
         } else
         {
            State.Assert(tessageType.Is<IExactlyOnceTommand>(),
                         () => $"Endpoint {connection.EndpointInformation.Id} advertises the tessage type {tessageType.FullName}, which no route can serve: TessageBus tommands route exactly-once only ({nameof(IExactlyOnceTommand)} — see src/Compze.Tessaging/dev_docs/tevent-delivery-model.md). Every advertised type must get a route — a subscription must never be silently dropped.");
            _tommandHandlerRoutes.Add(tessageType, connection);
         }
      }

      _teventSubscriberRouteCache.Clear();
   }

   //Update, not Read, deliberately: waiters must wake to observe the stop - their conditions' lookups assert against it, which
   //is how a shutdown propagates out of a wait immediately.
   public void Stop() => _monitor.Update(() => _stopped = true);

   ContractAsserter AssertNotStopped() => State.Assert(!_stopped, () => "router is stopped");

   public bool HasLiveConnectionTo(EndpointId endpointId) => _monitor.Read(() => _connections.ContainsKey(endpointId));

   public ITessagingInboxConnection? LiveConnectionToHandlerFor(Type tommandType) =>
      _monitor.Read(() =>
         AssertNotStopped().__(() => _tommandHandlerRoutes.GetValueOrDefault(tommandType)));

   public IReadOnlyList<TypermediaRoute> TypermediaRoutesFor(Type tessageType) =>
      _monitor.Read(() =>
      {
         AssertNotStopped();
         State.Assert(_endpointRegistry is not null,
                      () => "Remote typermedia navigation requires the registry the endpoint discovers other endpoints through — declare DiscoverEndpointsThrough/ParticipateIn on the endpoint's distributed feature. (An external client application navigates explicitly known addresses through the typermedia client router instead.)");
         return _typermediaHandlerRoutes.TryGetValue(tessageType, out var connections)
                   ? (IReadOnlyList<TypermediaRoute>)[..connections.Select(connection => new TypermediaRoute(connection.EndpointInformation.Id, connection.RemoteAddress))]
                   : [];
      });

   //Read despite the cache fill below: this monitor's read lock is exclusive, so the fill is safe, and a cache fill changes no
   //state a condition wait watches, so it must not wake the waiters the way an update-lock release would.
   public IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) =>
      _monitor.Read(() =>
      {
         AssertNotStopped();
         var wrapperTeventType = wrappedTevent.GetType();
         if(_teventSubscriberRouteCache.TryGetValue(wrapperTeventType, out var cached)) return cached;

         cached = [.._teventSubscriberRoutes
                    .Where(route => route.TeventType.IsInstanceOfType(wrappedTevent))
                    .Select(route => route.Connection)
                    .Distinct()]; //An endpoint is one subscriber however many of its advertised subscriptions match: it receives the tevent once and its own registry fans out to every matching handler.
         _teventSubscriberRouteCache[wrapperTeventType] = cached;
         return cached;
      });

   //The condition may call this router's own lookups: the monitor is reentrant, and the wait runs the condition on its own
   //dedicated thread, so the nested lock acquisitions are balanced re-entries, never contention.
   public async Task<bool> TryAwaitConnectionsOrPeerMemorySatisfyingAsync(Func<bool> condition, WaitTimeout patience) =>
      await _monitor.TryAwaitOnDedicatedThreadAsync(condition, waitTimeout: patience).caf();

   public void Dispose()
   {
      var alreadyDisposed = false;
      _monitor.Update(() =>
      {
         alreadyDisposed = _disposed;
         _disposed = true;
         _stopped = true;
      });
      if(alreadyDisposed) return;

      _reconcileLoopCancellation.Cancel();
      //Awaited outside the monitor: a reconciliation pass in flight takes the monitor to finish, so waiting for the loop while holding it would deadlock.
      _reconcileLoop?.WaitUnwrappingException();
      _reconcileLoopCancellation.Dispose();

      _monitor.Update(() => _connections.Values.DisposeAll());
   }
}
