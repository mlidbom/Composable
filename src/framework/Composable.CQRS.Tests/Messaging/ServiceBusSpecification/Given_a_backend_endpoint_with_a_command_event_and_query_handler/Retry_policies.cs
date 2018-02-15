using System;
using System.Transactions;
using Composable.Messaging.Buses;
using Composable.System;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Retry_policies_AtMostOnceCommand_when_command_handler_fails : Fixture
    {
        [SetUp] public void SendCommandThatFails()
        {
            var exceptionMessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
            MyCreateAggregateCommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionMessage));

            Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(() => ClientEndpoint.ExecuteRequest(session => RemoteNavigator.Post(MyCreateAggregateCommand.Create())));
        }

        [Test] public void ExactlyOnce_Event_raised_in_handler_does_not_reach_remote_handler()
        {
            MyRemoteAggregateEventHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, TimeSpanExtensions.Seconds(1))
                                                   .Should()
                                                   .Be(false, "event should not reach handler");
        }

        [Test] public void command_handler_is_tried_5_times() => MyCreateAggregateCommandHandlerThreadGate.Passed.Should().Be(5);

        [Test] public void ExactlyOnce_Event_raised_in_handler_reaches_local_handler_5_times() => MyLocalAggregateEventHandlerThreadGate.Passed.Should().Be(5);
    }
}
