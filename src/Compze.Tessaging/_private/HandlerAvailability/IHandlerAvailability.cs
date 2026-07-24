using Compze.Tessaging.TessageBus.Exceptions;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Endpoints.Exceptions;
using Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Tessaging._private.HandlerAvailability;

///<summary>The endpoint's waiting sends: a send whose type has no live, unambiguous route right now does not explode — it<br/>
/// waits, bounded by the endpoint's <see cref="HandlerAvailabilityPatience"/>, for the world to become right (a first contact,<br/>
/// a known peer's return, an ambiguity resolving), then proceeds normally; only exhausted patience fails loud, and the failure<br/>
/// says what was waited for, for how long, and what the peer memory remembers<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>).</summary>
///<remarks>Why it exists: every remote-facing request/response send used to race discovery at startup — sent before the<br/>
/// serving peer's advertisement was discovered, it exploded instantly, and applications were forced to hand-roll readiness<br/>
/// probes and retry loops around the framework. Waiting absorbs the startup race and steady-state churn (a handler endpoint<br/>
/// restarting mid-day creates a seconds-wide window with no route); it never absorbs a real misdeployment, which surfaces as<br/>
/// the loud, diagnostic patience-exhausted failure.</remarks>
///<remarks>Waiting is a condition wait, never polling: the availability condition is evaluated under the router's state lock<br/>
/// and re-evaluated the moment the routes or the peer memory could have changed<br/>
/// (<see cref="Routing.ITessagingRouter.TryAwaitConnectionsOrPeerMemorySatisfyingAsync"/>), so a send proceeds the instant the<br/>
/// world becomes right — zero added latency, zero recurring wakeups.</remarks>
interface IHandlerAvailability
{
   ///<summary>The address of the one connected endpoint whose advertisement handles the typermedia type<br/>
   /// <paramref name="tessageType"/> — waiting, within patience, for exactly one to be connected. No route after patience<br/>
   /// throws <see cref="NoHandlerForTypermediaTypeException"/> telling known-but-down from never-seen by the peer memory;<br/>
   /// several live routes still ambiguous after patience throw <see cref="MultipleHandlersForTypermediaTypeException"/><br/>
   /// naming the endpoints — never a silent pick.</summary>
   Task<EndpointAddress> AwaitAddressOfTypermediaHandlerForAsync(Type tessageType);

   ///<summary>The one endpoint an exactly-once tommand of type <paramref name="tommandType"/> binds to at send: the live<br/>
   /// handler when one is connected — current by definition — otherwise the sole remembered peer whose advertisement handles<br/>
   /// the type. A known-but-down handler binds immediately, never waited on: the row waits out the peer's absence in the<br/>
   /// outbox's storage. What waits, within patience, are the two states with no bindable receiver: a type nothing this<br/>
   /// endpoint has ever met serves — waiting for its first contact, after which the send binds and proceeds — and several<br/>
   /// remembered handlers with none live — waiting for one to connect (live is current by definition, resolving the<br/>
   /// replacement ambiguity). Exhausted patience throws<br/>
   /// <see cref="NoHandlerForTessageTypeException"/> / <see cref="MultipleHandlersForTessageTypeException"/>.</summary>
   ///<remarks>The wait strictly precedes the bind, so the exactly-once in-order guarantee is untouched: the tommand still<br/>
   /// binds exactly once, before its row is saved, and rides the bound pair's single ordered, receiver-deduped delivery<br/>
   /// stream — waiting only moves <em>when</em> that one bind happens in the two states that used to throw immediately.</remarks>
   Task<EndpointId> AwaitBindableReceiverOfAsync(Type tommandType);

   ///<summary>Readiness (see <see cref="IEndpoint.AwaitReadinessAsync"/>): completes when a handler for every type in<br/>
   /// <paramref name="readinessTypes"/> is available — the endpoint's own roster serves it, an exactly-once tommand type has<br/>
   /// a bindable receiver, or a request/response type has exactly one live route: precisely the availability a send would<br/>
   /// not have to wait for. Waits at most <paramref name="patience"/> — null means the endpoint's declared<br/>
   /// handler-availability patience — then throws <see cref="EndpointNotReadyWithinPatienceException"/> naming every type<br/>
   /// still unavailable and what the peer memory remembers about it.</summary>
   Task AwaitHandlersForAsync(ReadinessTypes readinessTypes, TimeSpan? patience);
}
