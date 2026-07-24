using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Peers;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging._private.Routing;

interface ITessagingRouter
{
    ///<summary>Connects to every endpoint the registry currently lists — minus the endpoint's own announced address<br/>
    /// (<paramref name="ownAddress"/>), because routes lead only to <em>other</em> endpoints: the roster serves in-roster<br/>
    /// tommands inline and the endpoint's own tevent subscriptions by in-boundary participation, so nothing self-addressed ever<br/>
    /// crosses the wire — and keeps reconciling the connections with<br/>
    /// the registry's membership until delivery stops: an endpoint that appears is connected, one whose address disappears is<br/>
    /// disconnected (its undelivered tessages wait in the outbox's storage for its return), and one that reappears at a new<br/>
    /// address — addresses are per-instance, identity is the <see cref="EndpointId"/> — has its connection replaced, its<br/>
    /// undelivered backlog following it. Reconciliation waits on the registry's change signal<br/>
    /// (<see cref="IEndpointRegistry.AwaitPossibleMembershipChange"/>), so membership changes propagate at signal latency. The<br/>
    /// first reconciliation completes before this method returns, so startup sees the registry's current membership connected.<br/>
    /// A null <paramref name="endpointRegistry"/> means the endpoint declared no discovery: it serves whatever reaches it, and<br/>
    /// its own roster serves its sends inline, but it connects to no other endpoint.</summary>
    Task StartMaintainingConnectionsAsync(IEndpointRegistry? endpointRegistry, EndpointAddress ownAddress);
    void Stop();
    void StartDelivery();
    void StopDelivery();
    ///<summary>The live connection to the endpoint whose current advertisement handles the tommand type<br/>
    /// <paramref name="tommandType"/> — null when no connected endpoint does. Deliberately liveness-only: a tommand binds to<br/>
    /// its one specific receiver at send time, preferring the live handler and falling back to the sole remembered one<br/>
    /// (see <see cref="IPeerRegistry.HandlerIdsFor"/>), so a handler being down never makes the send explode.</summary>
    ITessagingInboxConnection? LiveConnectionToHandlerFor(Type tommandType);
    ///<summary>Whether a live connection to the endpoint currently exists — the router's definition of the peer being up,<br/>
    /// which the internal specifications observe to script downtime and return deterministically.</summary>
    bool HasLiveConnectionTo(EndpointId endpointId);
    ///<summary>The connections to every endpoint whose advertised tevent subscriptions match <paramref name="wrappedTevent"/>. Advertised subscriptions are wrapper<br/>
    /// types, so matching is against the wrapper — pure type assignability. Which delivery leg the tevent travels to a matched subscriber is not routing's concern:<br/>
    /// the published tevent's own type decides that (see <see cref="IUnitOfWorkTeventPublisher"/>).</summary>
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IPublisherTevent<IRemotableTevent> wrappedTevent);

    ///<summary>The live routes for the typermedia tessage type <paramref name="tessageType"/>: every connected endpoint whose<br/>
    /// current advertisement handles it. Request/response routes liveness-only, and interpreting the count is the asker's<br/>
    /// business, not routing's — waiting sends wait, within patience, for the list to become a single entry<br/>
    /// (see <c>IHandlerAvailability</c>). Fails loud when the endpoint declared no discovery registry: with nothing to<br/>
    /// discover through there is nothing to navigate.</summary>
    IReadOnlyList<TypermediaRoute> TypermediaRoutesFor(Type tessageType);
}
