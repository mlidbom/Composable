using System;
using System.Threading.Tasks;
using Composable.Testing.Threading;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture_tests : Fixture
    {
        [Fact] public async Task Does_not_hang_if_command_handler_fails()
        {
            var throwTask = CommandHandlerThreadGate.ThrowOnNextPassThroughAsync(_ => new Exception(nameof(Does_not_hang_if_command_handler_fails)));
            Host.ClientBus.Send(new MyCommand());
            Assert.ThrowsAny<Exception>(() => TestDispose());
            await throwTask;
        }

        [Fact] public async Task Does_not_hang_if_command_handler_with_result_fails()
        {
            var throwTask = CommandHandlerWithResultThreadGate.ThrowOnNextPassThroughAsync(_ => new Exception(nameof(Does_not_hang_if_command_handler_fails)));
            var exceptionResult = Host.ClientBus.SendAsync(new MyCommandWithResult());
            Assert.ThrowsAny<Exception>(() => TestDispose());
            await Task.WhenAll(exceptionResult, throwTask);
        }

        [Fact] public void Does_not_hang_if_event_handler_fails()
        {
            EventHandlerThreadGate.SetPassThroughAction(_ => throw new Exception(nameof(Does_not_hang_if_command_handler_fails)));
            Host.ClientBus.Publish(new MyEvent());
            Assert.ThrowsAny<Exception>(() => TestDispose());
        }

        [Fact] public async Task Does_not_hang_if_QueryAsync_fails()
        {
            var throwTask = QueryHandlerThreadGate.ThrowOnNextPassThroughAsync(_ => new Exception(nameof(Does_not_hang_if_command_handler_fails)));
            var queryResult = Host.ClientBus.QueryAsync(new MyQuery());
            Assert.ThrowsAny<Exception>(() => TestDispose());
            await Task.WhenAll(queryResult, throwTask);
        }

        [Fact] public void Does_not_hang_if_query_fails()
        {
            QueryHandlerThreadGate.SetPassThroughAction(_ => throw new Exception(nameof(Does_not_hang_if_command_handler_fails)));
            Host.ClientBus.Query(new MyQuery());
            Assert.ThrowsAny<Exception>(() => TestDispose());
        }
    }
}
