using System.Linq;
using System.Transactions;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Transactions;
using Composable.Testing;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Transaction_policies : Fixture
    {
        [Fact] void Command_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            ClientEndpoint.ExecuteRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

            var transaction = CommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThrough.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Command_handler_with_result_runs_in_transaction_with_isolation_level_Serializable()
        {
            var commandResult = ClientEndpoint.ExecuteRequest(session => session.Post(MyAtMostOnceCommandWithResult.Create()));

            commandResult.Should().NotBe(null);

            var transaction = CommandHandlerWithResultThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThrough.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Event_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            ClientEndpoint.ExecuteRequest(session => session.Post(MyCreateAggregateCommand.Create()));

            var transaction = MyRemoteAggregateEventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                     .PassedThrough.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Query_handler_does_not_run_in_transaction()
        {
            ClientEndpoint.ExecuteRequest(session => session.Get(new MyQuery()));

            QueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThrough.Single().Transaction.Should().Be(null);
        }


        [Fact] void Calling_PostRemoteAsync_within_a_transaction_with_AtLeastOnceCommand_throws_TransactionPolicyViolationException() =>
            AssertThrows.Async<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteRequest(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create())))).Wait();

        [Fact] void Calling_PostRemoteAsync_within_a_transaction_AtLeastOnceCommand_throws_TransactionPolicyViolationException() =>
            AssertThrows.Exception<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteRequest(session => session.Post(MyAtMostOnceCommandWithResult.Create()))));


        [Fact] void Calling_PostRemoteAsync_without_a_transaction_with_ExactlyOnceCommand_throws_TransactionPolicyViolationException() =>
            AssertThrows.Exception<MessageInspector.TransactionPolicyViolationException>(() => ClientEndpoint.ExecuteRequest(session => session.Send(new MyExactlyOnceCommand())));

        [Fact] void Calling_GetRemoteAsync_within_a_transaction_with_Query_throws_TransactionPolicyViolationException() =>
            AssertThrows.Async<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteRequest(session => session.GetAsync(new MyQuery())))).Wait();

        [Fact] void Calling_GetRemote_within_a_transaction_with_Query_throws_TransactionPolicyViolationException() =>
            AssertThrows.Exception<MessageInspector.TransactionPolicyViolationException>(() => TransactionScopeCe.Execute(() => ClientEndpoint.ExecuteRequest(session => session.Get(new MyQuery()))));
    }
}
