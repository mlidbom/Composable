using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Peers._internal;

namespace Compze.Tessaging.Peers._private;

static class PeerMemoryRegistrar
{
   ///<summary>Registers the endpoint's one <see cref="IPeerMemory"/> — the public read-only view of the peer memory every<br/>
   /// transport-speaking endpoint keeps.</summary>
   public static IComponentRegistrar PeerMemory(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IPeerMemory>()
                                     .CreatedBy((IPeerRegistry peerRegistry) => new PeerMemory(peerRegistry)));
}

///<summary>The <see cref="IPeerMemory"/>: the <see cref="IPeerRegistry"/>'s remembered peers, exposed read-only.</summary>
class PeerMemory : IPeerMemory
{
   readonly IPeerRegistry _peerRegistry;

   internal PeerMemory(IPeerRegistry peerRegistry) => _peerRegistry = peerRegistry;

   public IReadOnlyList<RememberedPeer> Peers => _peerRegistry.Peers;
}
