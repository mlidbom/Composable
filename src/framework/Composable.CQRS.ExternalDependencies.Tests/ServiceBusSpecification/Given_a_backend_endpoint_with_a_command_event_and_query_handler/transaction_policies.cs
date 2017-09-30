using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Transaction_policies : Fixture
    {
        [Fact] void Command_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            Host.ClientBus.Send(new MyCommand());

            var transaction = CommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] async Task Command_handler_with_result_runs_in_transaction_with_isolation_level_Serializable()
        {
            var commandResult = await Host.ClientBus.SendAsync(new MyCommandWithResult());

            commandResult.Should().NotBe(null);

            var transaction = CommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Event_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            Host.ClientBus.Publish(new MyEvent());

            var transaction = EventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                     .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Query_handler_does_not_run_in_transaction()
        {
            Host.ClientBus.Query(new MyQuery());

            QueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThreads.Single().Transaction.Should().Be(null);
        }
    }
}
