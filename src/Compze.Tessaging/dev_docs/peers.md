# Peers: what an endpoint remembers about the endpoints it works with

This document takes a developer who is new to the Tessaging code from zero to understanding peers — the
endpoint's durable memory of the endpoints it converses with, and everything computed from that memory:
tevent fan-out membership, tommand receiver binding, queue-while-down, the advertisement lifecycle,
and handler availability (waiting sends and readiness). It is a companion to
[Tessaging](tessaging.md), which explains the paradigm, and to
[the tevent delivery model](tevent-delivery-model.md), which explains the delivery guarantees peer memory
feeds.

## The concepts

- **A peer** is another endpoint, as seen from this one: the unit of identity (`EndpointId`), lifecycle
  (first contact → advertisement updates), and expectation (`RequirePeers`). An endpoint
  is never its own peer — its own roster serves its tessages in-boundary.
- **A `RememberedPeer`** is what the memory holds per peer: its identity and its last-known
  **advertisement** — which remotable tessage types it handles and subscribes to, one advertisement
  covering every tessage kind, typermedia types beside TessageBus ones.
- **`IPeerRegistry`** is the memory itself. Sharply distinct from `IEndpointRegistry`, which is pure live
  discovery — who is reachable *right now*, at which per-instance address. The peer registry answers "who
  exists and what do they serve"; the endpoint registry answers "who is up and where".

The governing principle, in one line: **do not lose information unless it cannot be avoided. Dropping to a
lower guarantee is opt-in, never a default.** Two endpoints that converse without exactly-once guarantees
still depend on each other and know each other; endpoints whose relationships are genuinely disposable are
the edge case, declared per relationship (`DoNotQueueTeventsFor`), never assumed.

## Peer memory

- **Written from the discovery flow**: every advertisement fetch records the peer — creating it on first
  contact, replacing its stored advertisement wholesale on every later fetch
  (`IPeerRegistry.RecordAdvertisementAsync`). The router records every advertisement *before* it builds
  routes from it, so a live connection always belongs to a remembered peer, never the reverse.
- **Mirrored in memory, loaded at start**: reads (`Peers`, `SubscriberIdsFor`, `HandlerIdsFor`) are served
  from memory; the backing store is loaded in the endpoint's listening phase.
- **Durability follows the tier**: on an exactly-once endpoint the memory lives in the endpoint's prefixed
  table-set (`DurablePeerRegistry`) and survives restarts on both sides; on a database-less endpoint it
  lives for the life of the process (`ProcessLifetimePeerRegistry`), and the `RequirePeers` declaration is
  the durable memory a database-less endpoint cannot keep anywhere else — it survives restarts by being
  code. On the server engines the table-set sits in the domain database the endpoint joins; on SQLite it
  sits in an auxiliary database of its own, `«domain-database-name».PeerRegistry`, because a recording must
  commit while a domain transaction is open and SQLite's per-database write gate would otherwise queue it
  behind that transaction (`ISqlitePeerRegistryConnectionPool` tells the whole story).
- **Absence is not a lifecycle event.** Crash, liveness pruning, clean stop with retraction: none of them
  touch peer memory. They affect connections and delivery timing only — a peer is remembered while down,
  because absence is never forgetting.

## What the memory drives

### Exactly-once tevent fan-out: membership is remembered, not live

An exactly-once tevent's receivers are every remembered peer whose stored advertisement subscribes to it
(`SubscriberIdsFor`, matched by the same wrapper-type assignability the router's routes use) — never the
connections that happen to be live at publish time. The read happens inside the publish's transaction
against the same database, so fan-out membership commits or rolls back with the tevent. Live connections
decide only *when* delivery happens: the commit hook enqueues on the intersection with the live subscriber
connections (enqueue ⊆ persist by construction), and a remembered-but-disconnected peer is covered by the
recovery backlog its next connection loads. A subscribing peer that is down at publish time — a routine
rolling restart — receives the tevent on its return.

### Tommand receiver binding: bind at send, to one specific receiver

An exactly-once tommand binds to its one specific receiver at send time — the live handler when one is
connected, otherwise the sole remembered peer whose advertisement handles the type (`HandlerIdsFor`) — and
every tessage between a sender and a receiver rides that pair's single ordered, receiver-deduped delivery
stream: **exactly-once in-order holds by construction**. Consequences:

- A known-but-down handler receives the tommand on its return; the send never explodes on downtime.
- A bound tommand never enters another endpoint's stream: when a handler endpoint is replaced by a
  successor with a new `EndpointId`, in-flight tommands wait for their bound endpoint while new sends bind
  to the live successor. A tommand stranded by its receiver's permanent departure awaits explicit
  resolution ([the peer-administration roadmap](WIP/peer-administration.md)), never redelivery by inference.
- Several remembered handlers with none live is a diagnosable condition (a replacement whose retired peer
  is still remembered): the send waits — the moment one of them connects, binding to the live one is
  correct, so waiting legitimately resolves the ambiguity — and only exhausted patience throws
  `MultipleHandlersForTessageTypeException`, naming the peers and the remedy: bring the current handler up.
- A tommand whose handler is in the sender's own roster never reaches binding at all: it executes inline,
  in the sender's execution, per the consistency law.

### Best-effort queue-while-down: best effort means best

Every remembered subscriber has an in-memory queue on the publisher (`BestEffortTeventQueues`) that
outlives connections: tevents published while a known peer is down accumulate in publish order and drain on
its return. A delivery failure *pauses* the stream whole — the one tevent in flight is dropped, loudly;
everything behind it stays queued in order; draining resumes when the peer answers a tessage-free probe
(the endpoint-information query) or reconnects. The tier's loss surface is exactly: that single in-flight
tevent, publisher crash (memory is memory), and queue overflow.

- **The bound is size-based and generous: 10,000 tevents per peer.** Overflow fails the publish loud
  (`BestEffortTeventQueueOverflowException`, naming the peer and the bound) — backpressure loses nothing
  while shedding does. The reservation is taken inside the caller's transaction; rollback releases it.
- **`RequirePeers` queues before first contact**: everything published before a required peer's first
  advertisement is held — in order, within the bound — and the subset matching its subscriptions delivers
  when it is first met. Startup ordering stops mattering: nothing a required peer should see is lost to the
  discovery race.
- **`DoNotQueueTeventsFor` is the per-peer opt-down**: ephemerality is a property of the *relationship*,
  not the endpoint. A declared peer is delivered to only while connected; a delivery failure drops its
  stream whole. Mutually exclusive with requiring the same peer.
- **Request/response is excluded from queueing**: tueries and typermedia tommands have a caller awaiting a
  result — they get bounded-patience waiting (below), never a queue.

## The advertisement lifecycle

- **First contact**: connecting to a newly discovered endpoint creates its peer entry. Everything published
  before first contact is out of scope by definition — except tevents held under `RequirePeers`, which
  exist precisely to bridge it. Nothing can be owed a peer before it is first known — binding requires the
  peer to be remembered, and remembering happens at recording — so first contact finding rows already bound
  to the peer's id is an invariant violation.
- **Update**: every advertisement fetch replaces the stored advertisement wholesale. The peer registries
  notify the `IPeerLifecycleObserver` component set from inside the recording, consequences-first: the
  observers reconcile — in transactions of their own — before the advertisement is saved, and the in-memory
  mirror learns the peer last, so nothing binds to an advertisement that is not yet durable. Always before
  the peer's connection loads its recovery backlog, so what is stranded never enters a delivery stream. A
  crash between the steps loses nothing: reconciliation reruns on the peer's every later advertisement, and
  the next fetch records the advertisement again — the same rerun that covers a publish racing the
  recording. The one recording that can wait behind an open domain transaction is a replacement with
  something to strand — stranding writes the domain database; first contact and a growth never do.
- **Shrink**: a shrunk advertisement is the peer's own explicit declaration — an unsubscribe by the
  subscription's owner, nothing like absence. The outbox reconciles the peer's undelivered rows against
  every replaced advertisement: undelivered tessages of types the fresh advertisement no longer serves are
  **stranded** loudly (`IsStranded` on the dispatching row, excluded from the recovery backlog, never
  auto-un-stranded), kept and visible, never destroyed, awaiting explicit resolution
  ([the peer-administration roadmap](WIP/peer-administration.md)). The warning names the kind:
  - **Tevents**: the subscriber renounced interest — the tevents lost their audience by that audience's
    own choice.
  - **Tommands**: someone commanded an action that now has no handler — almost certainly a deployment
    error. The tommand is bound to its receiver, so a successor does not automatically receive it.

## Removing peers from memory: future design, deliberately unimplemented

Nothing forgets a peer today. A speculative decommission act existed and was deleted 2026-07-24 — it
bulk-destroyed exactly-once content, had no reviewed design, no admin surface, and no consumer beyond its
own tests. The staged design that will replace it — explicit per-kind resolution of orphaned tessages, an
archive as the destination for everything never-to-be-delivered, removal from memory as the final
refuses-while-anything-is-owed step — is recorded in
[the peer-administration roadmap](WIP/peer-administration.md), along with the safe steady states the
deletion leaves (bounded loud holds, inert undelivered rows, visible strands).

## Handler availability: waiting sends and readiness

Request/response cannot defer — a caller is awaiting an answer only a live handler can produce — so its
levers are how long a send waits, what the failure says, and what an application can await up front. Two
composing mechanisms:

### Waiting sends — implicit, per-call, bounded patience

A send whose type has no live, unambiguous route right now does not explode. It waits — through
`IHandlerAvailability`, a condition wait on the router's state (the availability condition is re-evaluated
the moment the routes or the peer memory could have changed, never polled) — for the world to become right:
a first contact, a known peer's return, an ambiguity resolving. Then it proceeds normally, the instant the
world does.
This is what absorbs steady-state churn: a handler endpoint restarting mid-day creates a seconds-wide
window with no route, and calls in that window wait it out.

- **The patience is the endpoint's handler-availability patience**: a flat 30 seconds unless the
  composition declares otherwise (`EndpointBuilder.HandlerAvailabilityPatience`).
- **The no-handler exception family is exclusively the patience-exhausted failure**
  (`NoHandlerForTessageTypeException` / `NoHandlerForTypermediaTypeException` /
  `MultipleHandlersForTessageTypeException`) — never thrown immediately — and the failure is classified
  from the same snapshot whose check exhausted the patience, telling **known-but-down** (naming the
  remembered peer that serves the type and is down) from **never-seen** (nothing this endpoint has ever met
  serves the type — naming the probable deployment error).
- What still throws immediately is *different exception types entirely*: programming-error-shaped failures
  (a non-remotable type, an unmapped type, a message-type-rule violation — waiting only delays the stack
  trace) and sends during shutdown.
- **The exactly-once cold-start bind waits too**: a tommand sent before its handler was ever met waits for
  the first contact and *then* binds — the wait strictly precedes the one bind-at-send, so the pair's
  single ordered receiver-deduped stream is untouched and exactly-once in-order holds unchanged.
- **The pure client keeps immediate throws**: `TypermediaClient`'s connections change only by its own
  explicit connects, so there is nothing to wait for.
- **One documented SQLite corner**: a sender whose transaction already holds the per-database write gate
  blocks the very advertisement recording that would satisfy its wait — the wait exhausts, the transaction
  rolls back releasing the gate, the recording lands, and a retry binds.

### Readiness — an explicit awaitable

`IEndpoint.AwaitReadinessAsync(ReadinessTypes, patience?)` completes when the endpoint can reach a handler
for every named type: its own roster serves it (in-boundary counts as available), an exactly-once tommand
type has a bindable receiver, or a request/response type has exactly one live route — precisely the
availability a send would not have to wait for, so the two mechanisms compose instead of overlapping.

- **Awaited on types, never on peers** — deployment topology stays out of application code. The type sets
  are `ReadinessTypes` reflection factories (`InAssemblyContaining`, `InNamespaceOf`), which admit only the
  remotable single-handler kinds; a set that is empty or would include anything else fails loud at
  composition. Tevents are excluded — multi-subscriber, no "available" concept, fully served by the
  queue/persist machinery above.
- **What readiness buys that waiting sends cannot**: who pays the wait (front-loaded to startup instead of
  the first unlucky caller), operations integration (an orchestrator's readiness probe wires to it), and
  fail-fast policy (a misdeployment surfaces once, at boot, instead of as every call timing out forever).
- Exhausted patience throws `EndpointNotReadyWithinPatienceException`, naming every type still unavailable
  with the known-but-down vs never-seen wording per type.

### Structurally excluded, by construction

`ILocalTypermediaNavigatorSession` is a different type that never crosses the wire, so in-process
navigation cannot race discovery; and a "self" send does not exist remotely at all — an in-roster tommand
executes inline in the sender's execution, so it has nothing to wait for.

## Where the behavior is pinned

`test/Compze.Tests.Integration/Tessaging/Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler/`
holds the peer-memory pins (`Peer_registry_tests`, `Tevent_delivery_to_peers_that_are_down_tests`,
`Tommand_receiver_binding_tests`, `Advertisement_shrink_tests`);
`test/Compze.Tests.Integration/Hosting/` the queue-while-down, required-peer, and opt-down specs;
`test/Compze.Tests.Integration/Tessaging/` the readiness and exactly-once-bind-wait specs; and
`test/Compze.Tessaging.Specifications/Typermedia/` the typermedia waiting-send specs.
