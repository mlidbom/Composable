using System;
using System.Transactions;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System;
using Composable.System.Transactions;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Exacly_once_guarantee_tests : Fixture
    {
        [Fact] void If_transaction_fails_after_successfully_calling_Send_command_never_reaches_command_handler()
        {
            try
            {
                TransactionScopeCe.Execute(() =>
                {
                    ClientEndpoint.ExecuteRequest(session => session.Send(new MyExactlyOnceCommand()));
                    throw new Exception("MyException");
                });
            }
            catch(Exception exception) when(exception.Message == "MyException") {}

            CommandHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, TimeSpanExtensions.Seconds(1))
                                    .Should()
                                    .Be(false, "command should not reach handler");
        }

        [Fact] void If_transaction_fails_after_successfully_calling_Publish_event_never_reaches_remote_handler_but_does_reach_local_handler()
        {
            var exceptionMessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
            MyCreateAggregateCommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionMessage));

            var something = Host.AssertThatRunningScenarioThrowsBackendAndClientTransaction<TransactionAbortedException>(() => ClientEndpoint.ExecuteRequest(session => Host.RemoteNavigator.Post(MyCreateAggregateCommand.Create())));

            something.BackendException.InnerException.Message.Should().Contain(exceptionMessage);
            something.FrontEndException.Message.Should().Contain(exceptionMessage);

            MyLocalAggregateEventHandlerThreadGate.Passed.Should().Be(1);

            MyRemoteAggregateEventHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, TimeSpanExtensions.Seconds(1))
                                  .Should()
                                  .Be(false, "event should not reach handler");
        }
    }
}
