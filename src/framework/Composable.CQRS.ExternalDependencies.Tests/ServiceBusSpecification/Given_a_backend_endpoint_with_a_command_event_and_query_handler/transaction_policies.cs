using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Transaction_policies : _Fixture
    {
        [Fact] void Command_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            _host.ClientBus.Send(new MyCommand());

            var transaction = _commandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] async Task Command_handler_with_result_runs_in_transaction_with_isolation_level_Serializable()
        {
            var commandResult = await _host.ClientBus.SendAsync(new MyCommandWithResult());

            commandResult.Should().NotBe(null);

            var transaction = _commandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Event_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            _host.ClientBus.Publish(new MyEvent());

            var transaction = _eventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                     .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Query_handler_does_not_run_in_transaction()
        {
            _host.ClientBus.Query(new MyQuery());

            _queryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThreads.Single().Transaction.Should().Be(null);
        }
    }
}
