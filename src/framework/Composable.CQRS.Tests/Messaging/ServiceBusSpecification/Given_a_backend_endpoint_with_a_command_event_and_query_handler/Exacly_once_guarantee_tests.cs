using System;
using System.Threading.Tasks;
using System.Transactions;
using Composable.System;
using Composable.System.Threading;
using Composable.System.Transactions;
using Composable.Testing;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Exacly_once_guarantee_tests : Fixture
    {
        [Fact] void If_transaction_fails_SendAsync_command_never_reaches_command_handler_and_awaiting_result_throws_TransactionAbortedException()
        {
            Task<MyCommandResult> commandResultTask = null;

            try
            {
                TransactionScopeCe.Execute(() =>
                {
                    commandResultTask = Host.ClientBus.SendAsync(new MyCommandWithResult());
                    throw new Exception("MyException");
                });
            }
            catch(Exception exception) when (exception.Message == "MyException"){}

            CommandHandlerWithResultThreadGate.TryAwaitPassededThroughCountEqualTo(1, TimeSpanExtensions.Seconds(1))
                                              .Should()
                                              .Be(false);

            AssertThrows.Exception<TransactionAbortedException>(() => commandResultTask.ResultUnwrappingException());
        }

        [Fact] void If_transaction_fails_Send_command_never_reaches_command_handler()
        {
            try
            {
                TransactionScopeCe.Execute(() =>
                {
                    Host.ClientBus.Send(new MyCommand());
                    throw new Exception("MyException");
                });
            }
            catch(Exception exception) when (exception.Message == "MyException"){}

            CommandHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, TimeSpanExtensions.Seconds(1))
                                              .Should()
                                              .Be(false);
        }

        [Fact] void If_transaction_fails_Published_event_never_reaches_handler()
        {
            try
            {
                TransactionScopeCe.Execute(() =>
                {
                    Host.ClientBus.Publish(new MyEvent());
                    throw new Exception("MyException");
                });
            }
            catch(Exception exception) when (exception.Message == "MyException"){}

            EventHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, TimeSpanExtensions.Seconds(1))
                                    .Should()
                                    .Be(false);
        }
    }
}
