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
      //Its own transaction, never the caller's: recording an advertisement is fact-keeping - the fetch happened - and must not
      //roll back with any unrelated ambient transaction. The lifecycle observers are notified inside it, so the recorded
      //advertisement and its consequences - the outbox pruning what a shrink orphaned - commit or roll back together.
      await TransactionScopeCe.SuppressAmbientAsync(async () => await TransactionScopeCe.ExecuteAsync(async () =>
      {
         await _sqlLayer.SaveAdvertisementAsync(advertisement.Id, advertisement.HandledTessageTypes).caf();
         await _lifecycleObservers.NotifyAdvertisementRecordedAsync(previous, peer).caf();
      }).caf()).caf();
      _rememberedPeers.Remember(peer);
   }

   public IReadOnlyList<RememberedPeer> Peers => _rememberedPeers.Peers;

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) => _rememberedPeers.SubscriberIdsFor(wrappedTevent);

   public IReadOnlyList<EndpointId> HandlerIdsFor(Type tessageType) => _rememberedPeers.HandlerIdsFor(tessageType);
}
