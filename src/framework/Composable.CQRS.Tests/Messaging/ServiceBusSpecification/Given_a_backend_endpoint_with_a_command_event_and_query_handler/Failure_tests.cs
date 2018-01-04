using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.Testing;
using Composable.Testing.Threading;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Failure_tests : Fixture
    {
        [Fact] public async Task If_command_handler_with_result_throws_awaiting_SendAsync_throws()
        {
            CommandHandlerWithResultThreadGate.ThrowOnPassThrough(_thrownException);
            await AssertThrows.Async<Exception>(async () => await Host.ClientBus.SendAsync(new MyCommandWithResult()));
        }

        [Fact] public async Task If_query_handler_throws_awaiting_QueryAsync_throws()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            await AssertThrows.Async<Exception>(() => Host.ClientBus.QueryAsync(new MyQuery()));
        }

        [Fact] public void If_query_handler_throws_Query_throws()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            Assert.ThrowsAny<Exception>(() => Host.ClientBus.Query(new MyQuery()));
        }

        public override void Dispose()
        {
            Assert.ThrowsAny<Exception>(() => Host.Dispose());
            base.Dispose();
        }

        readonly IntentionalException _thrownException = new IntentionalException();
        class IntentionalException : Exception {}
    }
}
