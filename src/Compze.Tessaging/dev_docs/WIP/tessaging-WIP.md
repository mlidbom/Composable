# Tessaging work in progress

The hub for ongoing Tessaging work. Current-state documentation — how the project actually works — lives in
[the parent dev_docs folder](../); this folder holds only the efforts in flight, and [DONE](../DONE/) holds
the completed ones.

## Ongoing work

**None.** The Tessaging migration is executed in full — all ten phases of
[the migration plan](../DONE/tessaging-migration-plan.md), the last (the Host demotion) on 2026-07-18 — and
the current-state docs are the living truth.

## Open items awaiting their own efforts

- **Distributed quiescence** — the awaitable "no volatile work is in flight across the suite → safe to
  stop", readiness's sibling on the way down. Sketched in
  [the style-substrate evaluation, question 6](../DONE/style-substrate-and-hosting-evaluation.md); to be
  designed properly in its own effort.
- **The ultimate home of `EndpointHost` and `InterprocessEndpointRegistry`** — the homes decision deferred
  this to the Host demotion; the demotion kept the project shapes unchanged, so the question is now free-
  standing: `Compze.Hosting` holds a ~60-line convenience and the same-machine registry, and whether either
  deserves a different home is an open naming/homes conversation.
- **Awaiting a subscriber's first contact on the production surface** — exactly-once tevent fan-out
  membership is the remembered subscribers, and first contact is the boundary; readiness covers only
  single-handler kinds and `RequirePeers` only the best-effort queues, so a production application that
  publishes exactly-once tevents right at startup and must not lose them to the discovery race has no
  first-class await for "my known subscriber has been met" — today it would poll `IPeerRegistry.Peers`, and
  the testing host wraps exactly that as `AwaitEndpointsHaveMetEachOtherAsync`. Whether this deserves a
  production surface is an open design question.
- **The public website docs** (`src/Websites/Website/docs/tessaging/`) still speak dead ServiceBus
  vocabulary, and `_docs/introduction.md` + its `TessageHandling.cs` samples predate the declaration idiom —
  a pass of their own.
- **The storage-drop administration act** — decommissioning an endpoint's storage (dropping its prefixed
  table-set, deleting its catalog entry) is a settled design equation parked as a todo at
  `ITessagingSqlLayer.IEndpointCatalogSqlLayer`, awaiting its first consumer.

## Current-state documentation (the parent folder)

- [tessaging.md](../tessaging.md) — what Tessaging is: the paradigm and its two siblings, the
  consistency law, the engine, endpoints, storage, topology, administration.
- [tevent-delivery-model.md](../tevent-delivery-model.md) — how tevents travel: the delivery ladder,
  publishing, subscribing, observation, ordering.
- [peers.md](../peers.md) — the endpoint's memory of its peers and everything computed from it:
  fan-out membership, receiver binding, queue-while-down, advertisement lifecycle, waiting
  sends and readiness.
- [storage.md](../storage.md) — the domain database: per-endpoint table-sets, the endpoint
  catalog, the process lock, schema creation.
- [src/Compze.Hosting/dev_docs/hosting.md](../../../Compze.Hosting/dev_docs/hosting.md) — what
  an endpoint and a host are; production and testing hosting.
- [src/Compze.Hosting/dev_docs/wip/same-machine-hosting.md](../../../Compze.Hosting/dev_docs/wip/same-machine-hosting.md)
  — same-machine hosting: the named-pipe transport, the interprocess registry, and the router's
  reconciliation.

## References

- [src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md](../../../TODO/type-assignability-routing-and-publisher-identifying-tevents.md)

### Completed efforts

- [DONE/tessaging-migration-plan.md](../DONE/tessaging-migration-plan.md) — the ten-phase migration, executed
  in full: debris → homes → one router → the engine → endpoint types → observation → synchrony → readiness →
  storage → the Host demotion.
- [DONE/tessaging-target-design.md](../DONE/tessaging-target-design.md) — the destination, built in full: the
  design record behind the current-state docs.
- [DONE/durable-peer-topology.md](../DONE/durable-peer-topology.md) — peer memory, queue-while-down, shrink,
  decommission.
- [DONE/readiness-and-waiting-sends.md](../DONE/readiness-and-waiting-sends.md) — waiting sends and the
  readiness awaitable.
- [DONE/style-substrate-and-hosting-evaluation.md](../DONE/style-substrate-and-hosting-evaluation.md) — the
  evaluation whose verdicts drove the harmonization: Tessaging the common paradigm, the feature machinery's
  death, the concrete endpoint types, the Host demotion.
- [DONE/typermedia-tessaging-split/](../DONE/typermedia-tessaging-split/) — the split that preceded the
  harmonization.
- [DONE/client-endpoint-entanglement.md](../DONE/client-endpoint-entanglement.md),
  [DONE/remove-iclient.md](../DONE/remove-iclient.md) — earlier untanglings.

### Public documentation

- [src/Compze.Tessaging/_docs/introduction.md](../../_docs/introduction.md)
