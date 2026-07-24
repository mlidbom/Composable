using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Peers;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.TypeIdentifiers;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>The peer memory (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>): an exactly-once endpoint durably remembers the<br/>
/// peers it has met — each peer's identity and last-known advertisement — read through <see cref="IPeerMemory.Peers"/>.<br/>
/// That the memory is durable, not merely mirrored in memory, is pinned by the restart conversations: every specification that<br/>
/// rebuilds the host and finds the down peer still remembered (tevent delivery to peers that are down, the advertisement<br/>
/// shrinks) reads it back from the endpoint's database.</summary>
public class Peer_registry_tests : EndpointHostTestBase
{
   [PCT] public void After_start_the_backend_remembers_the_remote_endpoint_with_the_tommand_type_it_handles_in_its_advertisement()
   {
      var rememberedPeer = BackendEndPoint.ServiceLocator.Resolve<IPeerMemory>().Peers
                                          .Single(peer => peer.Id.Equals(RemoteEndpointDeclaration.Id));

      var typeMap = BackendEndPoint.ServiceLocator.Resolve<ITypeMap>();
      rememberedPeer.HandledTessageTypes.Contains(typeMap.GetId(typeof(MyExactlyOnceTommandHandledByTheRemoteEndpoint)).CanonicalString).Must().BeTrue();
   }

   ///<summary>The acceptance pin that the substrate harmonization is real: one advertisement, remembered once, covers every<br/>
   /// tessage kind — so peer memory covers typermedia types exactly as it covers TessageBus ones.</summary>
   [PCT] public void The_remote_endpoint_remembers_the_backends_typermedia_types_in_the_same_advertisement_as_its_tessaging_ones()
   {
      var rememberedBackend = RemoteEndpoint.ServiceLocator.Resolve<IPeerMemory>().Peers
                                            .Single(peer => peer.Id.Equals(BackendEndpointDeclaration.Id));

      var typeMap = BackendEndPoint.ServiceLocator.Resolve<ITypeMap>();
      IReadOnlyList<Type> backendTypermediaTypes = [typeof(MyCreateTaggregateTommand), typeof(MyTuery), typeof(MyAtMostOnceTypermediaTommandWithResult)];

      backendTypermediaTypes.All(typermediaType => rememberedBackend.HandledTessageTypes.Contains(typeMap.GetId(typermediaType).CanonicalString)).Must().BeTrue();
   }
}
