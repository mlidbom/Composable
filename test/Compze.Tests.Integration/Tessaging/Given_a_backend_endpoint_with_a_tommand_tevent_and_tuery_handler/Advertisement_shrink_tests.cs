using Compze.Must;
using Compze.DependencyInjection;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>A shrunk advertisement is the peer's own explicit declaration — an unsubscribe by the subscription's owner — and<br/>
/// what the outbox owes the peer follows it (see the advertisement lifecycle in <c>src/Compze.Tessaging/dev_docs/peers.md</c>):<br/>
/// undelivered tessages of types the peer no longer serves — renounced tevent subscriptions, no-longer-handled tommand types —<br/>
/// are stranded, loudly: kept, but excluded from the recovery backlog until resolved explicitly, because delivering them would<br/>
/// hand the peer tessages it just declared it does not serve. Both halves are scripted as the real conversation:<br/>
/// the Backend meets the Remote endpoint, the Remote endpoint goes down, and it returns with the shrunk advertisement — the<br/>
/// deployment where an endpoint keeping its identity dropped a handler or a subscription.</summary>
[LongRunning]
public class Advertisement_shrink_tests : EndpointHostTestBase
{
   [PCT] public async Task A_tevent_published_while_its_subscriber_is_down_is_not_delivered_when_the_subscriber_returns_having_renounced_its_subscription_while_the_kept_tommand_is()
   {
      //The Backend met the Remote endpoint when the host started; rebuild the host without it: the subscriber is now down.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //Published while Remote is down: its remembered taggregate subscription makes the outbox owe it the tevent.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      //Bound at send to the remembered Remote endpoint - the control proving recovery delivery runs when it returns.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //Both tessages are undelivered: awaiting rest would wait for Remote's return.
      await StartHostWithTheRemoteEndpointReturningHavingRenouncedItsTeventSubscriptionsAsync();

      //The kept tommand delivers...
      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
      //...while the tevent, whose subscription the returned advertisement renounced, is not delivered to an endpoint that no
      //longer subscribes.
      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();
   }

   [PCT] public async Task A_tommand_bound_to_a_down_handler_that_returns_no_longer_handling_its_type_is_not_delivered_to_it_while_its_kept_tevent_subscriptions_are()
   {
      //The Backend met the Remote endpoint - the handler of the tommand's type - when the host started; rebuild the host without it: the handler is now down.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //Bound at send to the remembered Remote endpoint - the sole remembered handler of the type.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());
      //Published while Remote is down: its kept taggregate subscription must still deliver on its return - the control proving recovery delivery runs.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //Both tessages are undelivered: awaiting rest would wait for Remote's return.
      await StartHostWithTheRemoteEndpointReturningNoLongerHandlingItsTommandAsync();

      //The kept subscription's tevent delivers...
      MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
      //...while the tommand, whose type the returned advertisement no longer handles, is stranded rather than delivered to an
      //endpoint that would fail it - were it delivered, the failed handling would surface when the host disposes.
      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();
   }
}
