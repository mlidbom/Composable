using Compze.Tessaging.Endpoints;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Peers._private;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging.Peers._internal;

///<summary>The endpoint's memory of its peers — the endpoints it works with: each peer's identity and its last-known<br/>
/// advertisement (which remotable tessage types it handles and subscribes to). Written on every advertisement fetch, it is<br/>
/// what lets delivery membership stop depending on liveness: a peer is remembered while down — absence (a crash, a clean stop)<br/>
/// is never forgetting (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>).</summary>
///<remarks>Why it exists: without it, remote tevent fan-out was decided by the live connections at publish time, so a<br/>
/// subscriber that was down when a tevent was published — a routine rolling restart sufficed — silently never received it.</remarks>
///<remarks>Every transport-speaking endpoint registers exactly one, through the distributed Tessaging core. Durability follows<br/>
/// the foundation: with Tessaging persistence declared, peer memory lives in the endpoint's prefixed table-set in the domain<br/>
/// database it joins and survives restarts (<see cref="DurablePeerRegistry"/> — a peer, once met, stays remembered: nothing<br/>
/// forgets a peer until the peer-administration design exists, see <c>src/Compze.Tessaging/dev_docs/WIP/peer-administration.md</c>);<br/>
/// on a database-less endpoint it lives in memory for the life of the process (<see cref="ProcessLifetimePeerRegistry"/>).</remarks>
interface IPeerRegistry
{
   ///<summary>Records <paramref name="advertisement"/> as the advertising peer's current one, replacing what was stored —<br/>
   /// creating the peer on first contact.</summary>
   Task RecordAdvertisementAsync(EndpointInformation advertisement);

   ///<summary>Every remembered peer, with its last-known advertisement. Served from memory: the registry mirrors its backing<br/>
   /// store, loaded at start and updated on every <see cref="RecordAdvertisementAsync"/>.</summary>
   IReadOnlyList<RememberedPeer> Peers { get; }

   ///<summary>The <see cref="EndpointId"/> of every remembered peer whose last-known advertisement subscribes to<br/>
   /// <paramref name="wrappedTevent"/> — remote tevent fan-out's membership: decided by remembered advertisement, never<br/>
   /// by liveness, so a subscribing peer that is down at publish time is still fanned out to and receives the tevent on its<br/>
   /// return.</summary>
   ///<remarks>Subscriptions match by the same advertised-wrapper-type assignability the router's routes apply<br/>
   /// (<see cref="ITessagingRouter.SubscriberConnectionsFor"/>), and the router records every<br/>
   /// advertisement before it builds routes from it — so a live subscriber's connection always belongs to a listed peer,<br/>
   /// never the reverse.</remarks>
   IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent);

   ///<summary>The <see cref="EndpointId"/> of every remembered peer whose last-known advertisement handles the remotable<br/>
   /// single-handler type <paramref name="tessageType"/> — an exactly-once tommand, a typermedia tommand, or a tuery — matched<br/>
   /// exactly, the way the router's routes match these kinds. Exactly one entry is the known-but-down handler; none means<br/>
   /// nothing this endpoint has ever met serves the type (never-seen); more than one is a handler replacement whose retired<br/>
   /// peer is still remembered. Two askers: an exactly-once tommand binds to its one specific receiver at send time, and<br/>
   /// when no handler is live this list is where the receiver comes from; and waiting sends and readiness compute their<br/>
   /// known-but-down vs never-seen availability and failure wording from it, for every single-handler kind. The endpoint<br/>
   /// itself never asks: a peer is another endpoint, and an in-roster tessage is served in-boundary — an in-roster tommand<br/>
   /// executes inline in the sender's execution and never reaches the outbox's receiver binding at all.</summary>
   IReadOnlyList<EndpointId> HandlerIdsFor(Type tessageType);

   ///<summary>Initializes the registry's backing store, if any, and loads the remembered peers into memory. Runs in the<br/>
   /// endpoint's listening phase, before any endpoint in the host starts sending.</summary>
   Task StartAsync();
}
