using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Peers;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>Exactly-once tevent fan-out chooses its receivers from the peer registry's remembered peers, never from who happens<br/>
/// to be connected (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>): a subscribing peer that is down at publish time — a<br/>
/// routine rolling restart suffices — must still receive every tevent published in its absence when it returns.</summary>
[LongRunning]
public class Tevent_delivery_to_peers_that_are_down_tests : EndpointHostTestBase
{
   [PCT] public async Task A_tevent_published_while_a_remembered_subscriber_is_down_is_delivered_when_the_subscriber_returns()
   {
      //The Backend met the Remote endpoint when the host started; rebuild the host without Remote: the subscriber is now down.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //Down is not forgotten: the Backend's peer registry loaded Remote and its subscriptions from the Backend's database.
      BackendPeerMemory.Peers.Select(peer => peer.Id).Must().Contain(RemoteEndpointDeclaration.Id);

      //Publishes IMyTaggregateTevent - which Remote subscribes to - exactly-once, while Remote is down.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //The tevent is undelivered: awaiting rest would wait for Remote's return.
      await StartHostAsync();

      MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   IPeerMemory BackendPeerMemory => BackendEndPoint.ServiceLocator.Resolve<IPeerMemory>();
}
