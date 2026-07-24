# The Compze tevent delivery model

This document takes a developer who is new to Compze from zero to understanding how tevents are delivered —
what delivery guarantees exist, where each guarantee comes from, how publishing and subscribing work, and
what happens when a tevent crosses a process boundary. It is a companion to
[Tessaging](tessaging.md), which explains the paradigm the delivery machinery serves, and to
[Compze hosting](../../Compze.Hosting/dev_docs/hosting.md), which explains what an endpoint *is*; this
document explains how tevents travel between the code that publishes them and the handlers that receive them.

## The big picture

### Tevents, briefly

A tevent is a type-routed event: subscribers subscribe to a tevent *type*, and receive every published
tevent whose runtime type is assignable to it — `IUserImported : IUserRegistered : IUserEvent` means an
`IUserEvent` subscriber hears about imports and registrations without either publisher or subscriber knowing
the other exists. Publishing is 1:N fan-out to unknown subscribers. (Tommands are the 1:1 opposite — one
sender, exactly one handler — and follow different rules; see [Tommands are different](#tommands-are-different).)

### The two orthogonal axes

Two independent questions decide how a tevent travels:

- **Remotability** — may this tevent leave the process at all?
- **Delivery guarantee** — what promise does its delivery make?

These are orthogonal by design. `IRemotableTevent` says only "may cross an endpoint boundary" and promises
nothing about how; the guarantees are separate markers (`IMustBeSentTransactionally`,
`IMustBeHandledTransactionally`) and tiers (`IAtMostOnceTessage`, `IExactlyOnceTessage`) layered on top.
Conflating the axes — assuming "crosses a boundary" implies "durable and transactional" — was the historical
mistake this model corrects.

### The one design rule

Everything below follows from a single rule:

> **A tevent's type states its delivery contract. The endpoint's wiring supplies the delivery legs. A
> subscription may only opt down — and only all the way.**

Unpacked:

- **The type is the contract.** Neither the publisher nor the subscriber picks a tevent's guarantee; the
  tevent's own interfaces declare it, and every delivery edge honors it. Publishing an `IExactlyOnceTevent`
  *means* exactly-once delivery to its default subscribers — there is no parameter that weakens it.
- **Wiring supplies the legs, and missing legs fail loud.** Which delivery mechanisms an endpoint has
  (outbox, inbox, best-effort transport) is composition. A tevent whose contract needs a leg the endpoint did
  not wire is a loud setup/publish error — never a silent downgrade. Silently degrading a durability
  guarantee is data loss dressed as success.
- **Subscriptions opt down binarily or not at all.** A subscription either takes the tevent's full declared
  guarantee (the default) or opts all the way out to *observation* (the escape hatch, below). There is no
  menu of intermediate levels — deliberately: the only reason to want less than the declared guarantee is to
  shed cost, and any intermediate tier (say, dedup-without-durability) still needs the inbox's store to
  dedup, so it saves nothing. Full guarantee and free observation span the entire cost/guarantee frontier.

## The delivery ladder

Every (tevent, subscriber) edge lands on one of four rungs. Each rung is the one above it minus exactly one
property:

| Rung | What it is | Loses vs. the rung above |
|---|---|---|
| **Participation** | In-process, synchronous, in the *publisher's* transaction. A handler failure aborts the publish itself. | — (the strongest thing there is) |
| **Exactly-once** | Durable (outbox → inbox), handler in its *own* transaction, deduped, retried until handled. | Participation in the publisher's transaction |
| **Best-effort** | Across the wire, handler in its *own* transaction. No store, no dedup, no retry. | Guaranteed arrival |
| **Observation** | Committed facts only, off-thread, per-observer FIFO, no transaction. | Transactional handling |

Which rung an edge lands on:

- **Participation** is automatic for every local subscriber — it is what in-process delivery *is*.
- **Exactly-once vs. best-effort** is decided by the tevent's type: `IExactlyOnceTevent` travels the durable
  pipeline; a remotable tevent that is not exactly-once travels best-effort. The subscriber does not choose.
- **Observation** is the one subscription-side choice: the escape hatch drops any subscriber to this rung,
  whatever the tevent's type.

**Exactly-once membership is remembered, not live**: an exactly-once tevent's receivers are the peers whose
remembered advertisement subscribes to it (the peer registry — see [peers](peers.md)), never
the connections that happen to be live at publish time. A subscribing peer that is down when the tevent is
published receives it on its return.

Note that **best-effort still means transactional handlers**. A projection updating from a best-effort tevent
wants its *own* writes to be atomic; that is handler correctness, not a delivery guarantee, and the two must
not be conflated. Best-effort delivery drops guaranteed *arrival*; the handler still executes in its own scope
and transaction (the same shape as Typermedia's handler execution, which also pairs no-delivery-guarantees
with transactional handler execution). Only observation runs without a transaction.

## The tevent type hierarchy: contracts, not mechanisms

Defined in `Compze.Tessaging.Abstractions` (`_TessageTypes..Interfaces.cs`):

- **`ITevent`** — a type-routed event. By itself: participation only; never leaves the process.
- **`IRemotableTevent : ITevent`** — may cross endpoint boundaries; promises nothing further. **This is the
  best-effort tier.** There is deliberately no `IBestEffortTevent` marker: "best-effort" names the delivery
  *mechanism*, and putting a mechanism's name into a tessage type would re-entangle the two axes the model
  keeps apart. A plain `IRemotableTevent` and "delivered best-effort" are the same statement made once.
- **`IExactlyOnceTevent : IRemotableTevent, IExactlyOnceTessage`** — guaranteed arrival: sent
  transactionally through the outbox, handled transactionally through the inbox, deduped by its
  `IAtMostOnceTessage` `Id`, retried until handled.
- The markers underneath: `IMustBeSentTransactionally` (the send joins a transaction — requires the outbox),
  `IMustBeHandledTransactionally` (dedup/handling is atomic with the handler's effects — requires the
  inbox), and `IAtMostOnceTessage` (carries the `TessageId` infrastructure dedups on). At-most-once
  constrains only *handling*: an `IAtMostOnceTessage` may be *sent* best-effort, carrying its `Id`, and the
  receiver's dedup still guarantees ≤1 handling — the UI double-click case.

### The publisher-identifying wrapper carries identity, nothing else

Every tevent is wrapped in `IPublisherTevent<out TTevent>` before routing (routing matches on the
wrapper type; covariance makes a wrapper assignable to the wrapper-of every type its inner is assignable
to — see the routing model in `src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md`).
The wrapper carries **only publisher identity**. It declares no delivery-guarantee interfaces of its own:

- The guarantee is read off the *inner* tevent through covariance — a wrapper whose inner is exactly-once is
  an `IPublisherTevent<IExactlyOnceTevent>`, and that is what the outbox and router demand.
- The dedup identity is the inner tevent's `Id`, extracted once where the type is statically known (the
  outbox entry) and carried as transport-envelope data from there on.

One concept, one home: the tevent owns its guarantee, the wrapper owns publisher identity, the envelope owns
the dedup key.

## Publishing

There are two ways to publish, distinguished by the caller's relationship to a **unit of work** — one scope
paired with one ambient transaction, begun and completed together (see
`src/Compze.DependencyInjection/dev_docs/unit-of-work.md`). Code inside one publishes through the
unit-of-work publisher, which joins it; code outside any publishes through the independent publisher, which
gives each publish its own. The same duality repeats across the application-facing interfaces:
`IUnitOfWorkTommandSender`/`IIndependentTommandSender` for sending tommands, and
`ILocalTypermediaNavigatorSession`/`IIndependentLocalTypermediaNavigator` for navigating the local
typermedia API.

### `IUnitOfWorkTeventPublisher` — publish within the caller's unit of work

```csharp
public interface IUnitOfWorkTeventPublisher
{
   void Publish(ITevent tevent);        //strictly-local and best-effort kinds: sync stays first-class
   Task PublishAsync(ITevent tevent);   //every kind - the one form an IExactlyOnceTevent may use
}
```

Tevent-only, scoped, and the single fan-out point: it routes each tevent by the guarantee interfaces the
tevent's type declares:

- always → in-process delivery to this process's handlers, inline in the caller's transaction (participation);
- `IExactlyOnceTevent` → also the outbox (durable, delivered on commit);
- `IRemotableTevent` but not `IExactlyOnceTevent` → also the best-effort transport (best-effort, delivered on
  commit);
- not `IRemotableTevent` → local only.

Synchrony follows the type, and the sync/async pair mirrors it: `PublishAsync` serves every kind, awaiting
participation and the durable outbox write an exactly-once tevent's contract demands, while the synchronous
`Publish` serves the kinds whose contract keeps sync first-class and refuses an `IExactlyOnceTevent` loudly,
pointing at `PublishAsync` — publishing one writes durable rows inside the caller's transaction, which is
database I/O, async end to end by the type's contract.

It requires and honors the ambient transaction. Required: publishing with none present throws — there is no
unit of work to publish within, and the independent publisher is the interface for such callers. Honored: both
remote legs deliver on commit, so a rolled-back transaction never leaks a tevent — for the best-effort leg
that is "sent-on-commit without durability", not "sent transactionally". It auto-wraps a tevent published
without a publisher-identifying wrapper.

What this shape buys:

- **Anything can publish.** The tevent store is `IUnitOfWorkTeventPublisher`'s most common *client* — forwarding each
  committed taggregate tevent — not the owner of publishing. A service raising a best-effort monitoring
  signal, code with no taggregate in sight: same one interface.
- **Exactly-once is decoupled from the tevent store.** It requires only an ambient transaction (asserted)
  plus the outbox — the outbox row joins whatever transaction is present. Committing domain state and the
  tevent atomically is what the store *uses* this for, not what publishing *is*.
- **No publication mode.** Whether a tevent crosses the wire, and under which guarantee, is a property of
  the tevent's type — not an endpoint-wide mode declared once. An endpoint has one `IUnitOfWorkTeventPublisher`;
  which legs stand behind it is wiring, and a tevent needing an unwired leg fails loud.

### `IIndependentTeventPublisher` — publish as your own unit of work

The independent counterpart, for code that runs outside any unit of work — application code with no ambient
scope or transaction. A root-resolvable singleton, so a plain application class takes it as an ordinary
constructor dependency; each publish runs as its own unit of work around the unit-of-work publisher,
committed when the call (or the awaited `PublishAsync`) completes — with the same sync/async split.

Independence is asserted, not assumed — safety lives in asserts, not names: called from within an ambient
transaction it throws, because `TransactionScopeOption.Required` would silently *join* that transaction and
the publish would not stand alone. Inside a unit of work, publish through `IUnitOfWorkTeventPublisher`.

Before this interface existed, every outside-the-pipeline caller hand-built the unit of work from container
primitives — `ExecuteInIsolatedScope(scope => scope.Resolve<ITeventPublisher>().Publish(fact))` — forcing
application code to speak container language to say one domain verb.

### No publish-side escape hatch, deliberately

There is no "publish ignoring my transaction" interface: no real consumer for one exists — the imaginable
client would be tracing/monitoring infrastructure that must emit *now* regardless of the surrounding
transaction's fate — and without a consumer to arbitrate them, its correct semantics are contested (deliver
in the caller's scope under suppression vs. as its own unit of work; whether an
`IMustBeSentTransactionally` tevent may pass at all). A type for it would imply a settled abstraction that
does not exist.

A caller that genuinely needs the behavior composes it explicitly — suppress the ambient transaction and
publish through `IIndependentTeventPublisher`, whose no-ambient-transaction assert passes under
suppression — stating "this publish deliberately escapes my transaction's fate" at the call site. If a real
need materializes, its requirements design the abstraction.

## Subscribing

### Default: the tevent's declared guarantee

Registering a handler (`ForTevent<TTevent>(...)`) says only "this endpoint understands these tevents". The
delivery guarantee comes from each arriving tevent's type — the subscriber never picks it:

- a local publish → participation (synchronous, publisher's transaction);
- an arriving `IExactlyOnceTevent` → through the inbox: admitted in stream order on arrival, registered
  durably, handled in its own transaction under a row-level handling claim, retried until handled. The
  sender is acknowledged at admission, so a hard crash between admission and handler-commit gets no
  redelivery — the inbox's recovery scan at endpoint start re-enqueues every admitted but unhandled tessage,
  which is what makes acknowledged mean will-be-handled across crashes;
- an arriving best-effort tevent → direct dispatch in a unit of work of its own — atomic handler writes, but
  no dedup and no retry.

### `ObserveTevents` — observation

The one subscription-side choice: opt a handler all the way down to observation — watching, never
participating. Where the default registration delivers under the arriving tevent type's full guarantee, an
`ObserveTevents` observer stands outside the consistency model: it joins no transaction and its fate touches
no execution.

The observation contract — the fine print a subscriber accepts by dropping to this rung:

- **Committed facts only.** A locally published tevent is queued for its observers when its publishing unit
  of work commits — a rolled-back publish is never observed, because an observer must never watch an
  execution that never happened. An arriving tevent is queued on arrival: it is already a committed fact on
  its publisher. (For an exactly-once arrival that is the moment the inbox registers it, after dedup and
  before transactional processing; the *local handling's* later fate is a separate execution the observer
  deliberately ignores.)
- **Off-thread, isolated in both directions.** Dispatch runs on the engine's `TeventObservationDispatcher`,
  never on the publisher's or the transport's thread: an observer's failure never fails the caller, and an
  observer's latency never blocks it.
- **Per-observer FIFO.** Each observer has its own dispatch queue, so an observing read model sees tevents in
  the order they were queued — publish order locally, arrival order remotely — while a slow observer delays
  only itself. Tevents a participation handler publishes in response to another are observed after their
  cause.
- **At most once, never duplicates.** The exactly-once path queues observers only on first registration, so
  the inbox's dedup shields observers too; the best-effort path delivers at most once by nature.
- **Ordering relative to transactional handlers is unspecified.** Observation is queued at commit/arrival and
  dispatched in its own time; the transactional handlers run when their pipeline schedules them. Races are
  possible.
- **A throwing observer is reported, never retried** — surfaced through the background-exception reporter
  (production logs loud; the testing host's disposal rethrows), not swallowed, not redelivered.
- **Never discarded.** The testing host's at-rest wait covers the observation queues — a test cannot pass
  with observation work in flight — and an endpoint's disposal drains them (after listening stops, while the
  container still serves the observers their scopes).

### Wiring rules — loud failure, one direction

- A subscription demanding *more* than the endpoint can deliver fails at setup: a subscription to an
  `IExactlyOnceTevent` — either kind — or an `IExactlyOnceTommand` handler on an endpoint with no inbox wired
  is an error. Observation is no exception, even though its dispatch needs no machinery: an observation
  subscription joins the advertisement, and observing a remote exactly-once tevent still requires *receiving*
  it exactly-once — the dedup shield the observation contract promises IS the inbox's. An endpoint that
  cannot honor a guarantee must not advertise for it.
- The advertisement asserts its own soundness at composition: every advertised type must be one the peers'
  routers can serve (tevent subscriptions as wrapper-of-remotable types; tommands exactly-once only) —
  anything else fails loud instead of becoming a silently dead subscription — and the routers assert the
  same contract again, route by route.
- The reverse never fails: an endpoint with the full durable pipeline still delivers a best-effort tevent
  under its declared contract. Capability above the contract is simply unused. And zero wired remote legs is
  the deliberately local composition — the engine with no endpoint — where participation serves every
  subscriber that exists.
- The dynamic remainder fails loud at receive: a *wide* subscription (say, to a plain `ITevent` interface)
  can match a remote publisher's exactly-once tevent no setup-time rule could see. Arriving exactly-once
  traffic on an endpoint without the inbox is refused — the request fails on the sender and the tessage stays
  safely undelivered in the sender's outbox — never silently downgraded.

## Ordering

> **Tevents sent to a given endpoint are delivered in the order they were sent. We never reorder what we
> send.**

On the exactly-once rung the order is enforced by construction, at both ends of the pair's delivery stream:

- **The sender assigns each tessage its delivery stream sequence number** inside the transaction that saves
  it — from a per-receiver counter row whose lock serializes the pair's commits, so sequence order is commit
  order whatever interleaving the sending transactions had. The connection's send queue is keyed by that
  sequence number: the loop always leads with the lowest-sequenced undelivered tessage, one in flight per
  destination, never looking past it until it is delivered and acknowledged. Pipelining (multiple in flight)
  is deliberately not implemented: it is only worth its complexity for high-latency links, and same-machine
  delivery is fast sequentially.
- **The receiver's inbox admits only in stream order.** Each delivery attempt declares its predecessor —
  the pair's previous still-deliverable-or-received stream member, freshly computed from the sender's durable
  dispatching rows, so a hole punched by sender-side stranding (an undelivered tessage whose type the
  receiver's shrunk advertisement renounced) is
  crossed exactly when the rows say it is real. The inbox admits a tessage iff the pair's admission high-water
  mark equals that declared predecessor; a tessage at or below the mark is acknowledged as a redelivered
  duplicate, and anything else is refused back into the sender's retry, which heals the stream by leading
  with the missing predecessor. No sender-side path — a commit hook racing the recovery backlog load, a
  restarted process, anything — can make the receiver register out of order.

Per rung:

- **Exactly-once: order also survives a sender restart.** Recovery reloads the undelivered backlog into the
  sequence-keyed send queue — re-establishing head-of-line on the oldest undelivered tessage — and the inbox
  admission rule holds regardless.
- **Best-effort: in order, across the subscriber's downtime.** Every remembered subscriber has an in-memory
  queue on the publisher that outlives connections: tevents published while the subscriber is down accumulate
  in publish order and its next connection drains them on its return — queue-while-down (see
  [peers](peers.md)). A delivery failure *pauses* the stream whole:
  the one tevent in flight at the failure is dropped, loudly — without receiver dedup a re-send could
  duplicate it, and nothing on this tier is ever re-sent — while everything queued behind it stays queued in
  order, resuming when the peer answers a tessage-free probe or reconnects. There is never a silent
  mid-stream skip that would deliver 54 after dropping 53: the tier's loss surface is exactly that single
  in-flight tevent, a publisher crash (memory is memory), and queue overflow (10,000 tevents per peer; the
  overflowing publish fails loud, naming the peer and the bound). The per-peer opt-down is the
  `DoNotQueueTeventsFor` declaration on the endpoint's composition surface: a peer the composition declares
  it keeps nothing for is delivered to only while connected — ephemerality is a property of the
  relationship, and every peer not declared gets queue-while-down. On the receive side, the acknowledgement
  is written after the handlers execute, so single-in-flight keeps *handling* in send order too.
- **Receive side: failing to *receive* is fatal.** An endpoint that cannot register an arriving guaranteed
  tevent in its inbox has a fatal bug — the system goes down rather than lose the tevent. Failing to
  *handle* (handler exceptions, retries exhausted) is a separate concern with its own policy.

## Tommands are different

Tommands are 1:1 — one sender, exactly one handler — and there the type dictates *everything*, with no
subscription-side election at all: an `IExactlyOnceTommand`'s type is its delivery contract (exactly-once,
transactional, asynchronous), sent through `IUnitOfWorkTommandSender.SendAsync`. A tommand whose handler is
in the sender's own roster never touches delivery machinery at all: it executes inline, in the sender's
execution — exactly-once by construction, per the consistency law (see
[Tessaging](tessaging.md)). A tommand whose handler lives elsewhere binds to its one
specific receiver at send time — the live handler when one is connected, otherwise the sole remembered peer
whose advertisement handles the type (see [peers](peers.md)) — so a
known-but-down handler receives the tommand on its return without the send exploding, while every tessage
between a sender and a receiver rides that pair's single ordered, receiver-deduped delivery stream:
exactly-once in-order holds by construction. An in-flight tommand therefore never delivers to a blue/green
successor — it waits for its own endpoint — while new sends bind to the live successor. The synchronous local ask has its own
truthful home in Typermedia's strictly-local tommand — see
[Compze hosting](../../Compze.Hosting/dev_docs/hosting.md). The subscription-side opt-down and the
best-effort tier described in this document are tevent concepts: they exist because tevent publishing is
decoupled 1:N fan-out, where the publisher must not decide the durability needs of subscribers it does not
know.
