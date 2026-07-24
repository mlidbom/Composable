using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Peers;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using Compze.Tessaging.Peers._private;

namespace Compze.Tessaging.TessageBus._private.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   ///<summary>The outbox's side of the peer lifecycle: keeps what the outbox owes a peer consistent with what the peer's own<br/>
   /// advertisement declares (see the advertisement lifecycle in <c>src/Compze.Tessaging/dev_docs/peers.md</c>). On every<br/>
   /// advertisement replacement it reconciles the peer's undelivered tessages against the fresh advertisement — a shrunk one is<br/>
   /// the peer's explicit renunciation, so undelivered tessages of types the peer no longer serves are stranded, loudly: kept<br/>
   /// and visible, excluded from delivery — delivering would hand the peer tessages it just declared it does not serve — and<br/>
   /// never destroyed. On first contact it asserts that nothing is owed to the peer: binding requires the peer to be<br/>
   /// remembered and remembering happens at recording, so a row already bound to a never-met identity is an invariant<br/>
   /// violation to fail loud on, never something to silently clean up.</summary>
   ///<remarks>Notified inside the transaction that persists the advertisement and always before the peer's connection loads its<br/>
   /// recovery backlog, so what is stranded here never enters a delivery stream. Reconciliation runs on every replacement, not<br/>
   /// only detected shrinks, deliberately: a publish fanning out on the not-yet-replaced peer memory can commit a row of a<br/>
   /// renounced type concurrently with the shrink that renounced it, and the rerun on the peer's next advertisement strands it.</remarks>
   ///<remarks>A stranded tessage stays stranded even when a later advertisement re-grows its type: while it was stranded, later<br/>
   /// tessages in the pair's stream kept delivering, so quietly re-scheduling it would deliver it out of order. Stranded<br/>
   /// tessages are kept indefinitely — visible, never destroyed; their explicit resolution awaits the peer-administration<br/>
   /// design (see <c>src/Compze.Tessaging/dev_docs/WIP/peer-administration.md</c>).</remarks>
   internal class PeerLifecycleObserver : IPeerLifecycleObserver
   {
      readonly ITessageStorage _storage;

      internal PeerLifecycleObserver(ITessageStorage storage) => _storage = storage;

      public async Task PeerMetForTheFirstTimeAsync(RememberedPeer peer)
      {
         //Read outside the recording's ambient transaction: an assertion probe must never enlist - on SQLite enlisting takes
         //the per-database write gate, and first contact must never queue behind an open handling transaction's gate.
         //The recovery-backlog read suffices as the probe: stranded rows require an advertisement replacement of a met peer,
         //so for a never-met peer the backlog covers everything that could possibly be bound.
         var rowsBoundToTheNeverMetPeer = await TransactionScopeCe.SuppressAmbientAsync(async () => await _storage.GetUndeliveredTessagesForEndpointAsync(peer.Id).caf()).caf();
         State.Assert(rowsBoundToTheNeverMetPeer.Count == 0,
                      () => $"First contact with peer {peer.Id}, but {rowsBoundToTheNeverMetPeer.Count} tessage(s) are already bound to its identity (types: {DistinctTypeNames(rowsBoundToTheNeverMetPeer.Select(it => it.TypeId))}). "
                          + "Binding requires the peer to be remembered, and remembering happens at recording - nothing can be owed a peer before it is first known, so these rows mean that invariant broke.");
      }

      public async Task PeerAdvertisementReplacedAsync(RememberedPeer previous, RememberedPeer current)
      {
         var undelivered = await _storage.GetUndeliveredTessagesForEndpointAsync(current.Id).caf();
         if(undelivered.Count == 0) return;

         List<ITessagingSqlLayer.UndeliveredTessage> renouncedTevents = [], noLongerHandledTommands = [];
         foreach(var tessage in undelivered)
         {
            var tessageType = tessage.TypeId.Type;
            if(tessageType.Is<ITevent>())
            {
               if(!current.SubscribesToTeventsOf(tessageType)) renouncedTevents.Add(tessage);
            } else if(!current.Handles(tessageType))
            {
               noLongerHandledTommands.Add(tessage);
            }
         }

         await StrandTheRenouncedTeventsAsync().caf();
         await StrandTheNoLongerHandledTommandsAsync().caf();
         return;

         async Task StrandTheRenouncedTeventsAsync()
         {
            if(renouncedTevents.Count == 0) return;
            await _storage.StrandUndeliveredTessagesAsync(current.Id, [..renouncedTevents.Select(it => it.TessageId)]).caf();
            this.Log().Warning($"Peer {current.Id}'s replaced advertisement no longer subscribes to the type(s) of {renouncedTevents.Count} undelivered tevent(s) bound to it. They lost their audience by that audience's own choice and are stranded: kept, visible, and not delivered, awaiting explicit resolution. Types: {DistinctTypeNames(renouncedTevents.Select(it => it.TypeId))}.");
         }

         async Task StrandTheNoLongerHandledTommandsAsync()
         {
            if(noLongerHandledTommands.Count == 0) return;
            await _storage.StrandUndeliveredTessagesAsync(current.Id, [..noLongerHandledTommands.Select(it => it.TessageId)]).caf();
            this.Log().Warning($"Peer {current.Id}'s replaced advertisement no longer handles the type(s) of {noLongerHandledTommands.Count} undelivered tommand(s) bound to it - someone commanded an action that now has no handler there, almost certainly a deployment error. They are stranded: kept, visible, and not delivered, awaiting explicit resolution. Types: {DistinctTypeNames(noLongerHandledTommands.Select(it => it.TypeId))}.");
         }
      }

      static string DistinctTypeNames(IEnumerable<TypeId> typeIds) => string.Join(", ", typeIds.Select(it => it.Type.FullName).Distinct());
   }
}
