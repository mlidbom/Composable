using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Testing;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture_tests : Fixture
    {
        [Fact] public void If_command_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            CommandHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            Host.ClientBus.PostRemote(new MyCommand());
            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        [Fact] public async Task If_command_handler_with_result_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
        {
            CommandHandlerWithResultThreadGate.ThrowOnPassThrough(_thrownException);
            await AssertThrows.Async<MessageDispatchingFailedException>(async () => await Host.ClientBus.PostRemoteAsync(new MyCommandWithResult()));

            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        [Fact] public void If_event_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            EventHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            Host.ClientBus.Publish(new MyEvent());
            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        [Fact] public async Task If_query_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            await AssertThrows.Async<MessageDispatchingFailedException>(() => Host.ClientBus.GetRemoteAsync(new MyQuery()));

            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        void AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException()
        {
            Assert.Throws<AggregateException>((Action)Host.Dispose)
                  .InnerExceptions.Single().Should().Be(_thrownException);
        }

        readonly IntentionalException _thrownException = new IntentionalException();
        class IntentionalException : Exception {}
    }
}
