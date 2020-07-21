using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.Testing;
using Composable.Testing.Threading;
using NUnit.Framework;
using Assert = Xunit.Assert;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Failure_tests : Fixture
    {
        [Test] public async Task If_command_handler_with_result_throws_awaiting_SendAsync_throws()
        {
            CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
            await AssertThrows.Async<Exception>(async () => await ClientEndpoint.ExecuteClientRequestAsync(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create())));
        }

        [Test] public async Task If_query_handler_throws_awaiting_QueryAsync_throws()
        {
            QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
            await AssertThrows.Async<Exception>(() => ClientEndpoint.ExecuteClientRequestAsync(session => session.GetAsync(new MyQuery())));
        }

        [Test] public void If_query_handler_throws_Query_throws()
        {
            QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
            Assert.ThrowsAny<Exception>(() => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery())));
        }

        public override void TearDown()
        {
            Assert.ThrowsAny<Exception>(() => Host.Dispose());
            base.TearDown();
        }

        readonly IntentionalException _thrownException = new IntentionalException();
        class IntentionalException : Exception {}

        public Failure_tests(string _) : base(_) {}
    }
}
