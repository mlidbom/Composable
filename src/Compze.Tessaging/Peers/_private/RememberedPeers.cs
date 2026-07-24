using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;
using Compze.Tessaging.Peers._internal;

namespace Compze.Tessaging.Peers._private;

///<summary>The in-memory peer memory behind every <see cref="IPeerRegistry"/> flavor: a monitor-guarded map of<br/>
/// <see cref="RememberedPeer"/>s that reads never leave. <see cref="DurablePeerRegistry"/> loads it from its storage at start<br/>
/// and updates it alongside every persisted advertisement; <see cref="ProcessLifetimePeerRegistry"/> has nothing else.</summary>
class RememberedPeers
{
   readonly IMonitor _monitor = IMonitor.New();
   IReadOnlyDictionary<EndpointId, RememberedPeer> _peers = new Dictionary<EndpointId, RememberedPeer>();

   internal void Remember(RememberedPeer peer) => _monitor.Locked(() => _peers = _peers.SetInCopy(peer.Id, peer));

   ///<summary>The remembered peer with this identity — null when no peer with it has been remembered.</summary>
   internal RememberedPeer? Find(EndpointId peerId) => _monitor.Locked(() => _peers.GetValueOrDefault(peerId));

   internal void ReplaceAllWith(IEnumerable<RememberedPeer> peers) => _monitor.Locked(() => _peers = peers.ToDictionary(peer => peer.Id));

   internal IReadOnlyList<RememberedPeer> Peers => _monitor.Locked(() => (IReadOnlyList<RememberedPeer>)[.._peers.Values]);

   internal IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._peers.Values.Where(peer => peer.SubscribesTo(wrappedTevent)).Select(peer => peer.Id)]);

   internal IReadOnlyList<EndpointId> HandlerIdsFor(Type tessageType) =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._peers.Values.Where(peer => peer.Handles(tessageType)).Select(peer => peer.Id)]);
}
