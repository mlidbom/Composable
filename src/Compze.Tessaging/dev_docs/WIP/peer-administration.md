# Peer administration — roadmap (design not started)

## Status: nothing implemented, deliberately

A speculative decommission surface (`IPeerAdministration.DecommissionAsync` — a single big-bang act that
removed a peer from memory and **deleted** everything held for it: undelivered exactly-once tessages, stranded
tommands, queued best-effort tevents) was implemented ahead of any consumer, any admin interface, and any
reviewed design, and was deleted 2026-07-24. Its only callers were tests of itself. This document records what
must exist instead, so the deletion leaves an honest todo rather than a hole.

## What the deletion leaves as (safe) steady states

- A peer, once met, stays remembered forever. A retired handler-replacement peer therefore leaves a permanent
  multiple-remembered-handlers ambiguity for its tommand types until the current handler is live
  (`MultipleHandlersForTessageTypeException` names this).
- A required-but-never-arriving peer's first-contact hold grows until the queue bound fails publishes loudly
  (`BestEffortTeventQueueOverflowException` — backpressure, not silent growth).
- Undelivered exactly-once tessages for a gone-forever peer wait in the outbox indefinitely: inert rows.
- Stranded tommands are kept, visible, indefinitely.

All acceptable for now: nothing is lost, everything is loud or inert.

## The design direction (from the 2026-07-24 review discussion)

Staged, never big-bang, and **the exactly-once tier never destroys content**:

1. **Obsolete subscriptions** end through the advertisement lifecycle (exists today: shrink handling).
2. **Orphaned tessages are resolved explicitly, per kind.** Tevents that lost their audience may be archived.
   A tommand is a *required domain action*: resolving one that will never execute (execute elsewhere, formally
   cancel, archive with a recorded decision) is a deliberate per-tommand/per-type act, never a bulk sweep.
3. **An archive (dead-letter) table-set** is the destination for every "this will never be delivered"
   determination — rows move there (atomically, same database), queryable, never deleted. The report of any
   resolving act points into it.
4. **Decommission, if it returns, is the final, almost-trivial step**: it *refuses while anything is owed*
   (archiving/resolving is how you make it owed-nothing first), then removes the peer row from memory.
   With that refusal, "rows owed to a never-met peer" becomes structurally impossible, which is what makes
   the first-contact assertion in `Outbox.PeerLifecycleObserver` a true invariant.
5. **Endpoint-storage removal** (dropping a retired endpoint's prefixed table-set + catalog entry, refused
   while its process lock is held) belongs to the same surface — see the todo in `_ITessagingSqlLayer`.
6. All of it surfaces through an actual administration interface, built when something real consumes it.
