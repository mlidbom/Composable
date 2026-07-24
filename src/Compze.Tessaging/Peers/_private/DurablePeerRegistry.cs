using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;
using Compze.Tessaging.Peers._internal;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging.Peers._private;

///<summary>The <see cref="IPeerRegistry"/> of an endpoint whose foundation declares Tessaging persistence: the durable peer<br/>
/// tables fronted by the in-memory <see cref="RememberedPeers"/>, so reads never touch the database and writes hit it once per<br/>
/// advertisement fetch. Peer memory survives restarts on both sides: a peer, once met, stays remembered.</summary>
[UsedImplicitly] class DurablePeerRegistry : IPeerRegistry
{
   readonly ITessagingSqlLayer.IPeerRegistrySqlLayer _sqlLayer;
   readonly ITypeMap _typeMap;
   readonly IReadOnlyList<IPeerLifecycleObserver> _lifecycleObservers;
   readonly RememberedPeers _rememberedPeers = new();

   internal DurablePeerRegistry(ITessagingSqlLayer.IPeerRegistrySqlLayer sqlLayer, ITypeMap typeMap, IReadOnlyList<IPeerLifecycleObserver> lifecycleObservers)
   {
      _sqlLayer = sqlLayer;
      _typeMap = typeMap;
      _lifecycleObservers = lifecycleObservers;
   }

   public async Task StartAsync()
   {
      await _sqlLayer.InitAsync().caf();
      _rememberedPeers.ReplaceAllWith((await _sqlLayer.GetPeersAsync().caf()).Select(peer => new RememberedPeer(peer.Id, peer.HandledTessageTypes, _typeMap)));
   }

   public async Task RecordAdvertisementAsync(EndpointInformation advertisement)
   {
      var peer = new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap);
      var previous = _rememberedPeers.Find(peer.Id);
      //Recording is fact-keeping - the fetch happened - so nothing here runs in the caller's transaction: with no ambient, the
      //recording cannot roll back with unrelated work, and on sqlite its reads never enlist and so never queue behind an open
      //domain transaction's write gate. The steps are ordered consequences-first:
      //1. The lifecycle observers reconcile against the fresh advertisement - the outbox stranding what a shrink renounced -
      //   in transactions of their own on the domain database.
      //2. The advertisement is saved, atomically - on sqlite to the peer registry's own database (its own write gate, so the
      //   save commits while a domain transaction is open - see the sqlite backend's ISqlitePeerRegistryConnectionPool).
      //3. The in-memory mirror learns the peer, so nothing binds to it before its advertisement is durable.
      //A crash between the steps loses no consequence: reconciliation reruns on every replacement, and the next fetch of this
      //peer's advertisement records it again - the same rerun that already covers a publish racing the recording.
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
      {
         await _lifecycleObservers.NotifyAdvertisementRecordedAsync(previous, peer).caf();
         await TransactionScopeCe.ExecuteAsync(async () => await _sqlLayer.SaveAdvertisementAsync(advertisement.Id, advertisement.HandledTessageTypes).caf()).caf();
      }).caf();
      _rememberedPeers.Remember(peer);
   }

   public IReadOnlyList<RememberedPeer> Peers => _rememberedPeers.Peers;

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) => _rememberedPeers.SubscriberIdsFor(wrappedTevent);

   public IReadOnlyList<EndpointId> HandlerIdsFor(Type tessageType) => _rememberedPeers.HandlerIdsFor(tessageType);
}
