using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Failure_tests : Fixture
    {
        IntentionalException thrownException = new IntentionalException();

        [Fact] public void If_command_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            CommandHandlerThreadGate.ThrowOnPassThrough(thrownException);
            Host.ClientBus.Send(new MyCommand());
            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(Host.Dispose);
        }

        [Fact] public async Task If_command_handler_with_result_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            CommandHandlerWithResultThreadGate.ThrowOnPassThrough(thrownException);
            var exceptionResult = Host.ClientBus.SendAsync(new MyCommandWithResult());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(Host.Dispose);

            (await Assert.ThrowsAsync<IntentionalException>(async () => await exceptionResult)).Should().Be(thrownException);
        }

        [Fact] public void If_event_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            EventHandlerThreadGate.ThrowOnPassThrough(thrownException);
            Host.ClientBus.Publish(new MyEvent());
            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(TestDispose);
        }

        [Fact] public async Task If_query_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(thrownException);
            var queryResult = Host.ClientBus.QueryAsync(new MyQuery());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(TestDispose);

            (await Assert.ThrowsAsync<IntentionalException>(async () => await queryResult)).Should().Be(thrownException);
        }

        [Fact] public void If_query_handler_throws_Query_and_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(thrownException);

            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(
                () => Host.ClientBus.Query(new MyQuery()),
                TestDispose);
        }


        void AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(params Action[] actions)
        {
            foreach(var action in actions)
            {
                Assert.Throws<AggregateException>(action)
                      .InnerExceptions.Single().Should().Be(thrownException);
            }
        }

        class IntentionalException : Exception {}
    }
}
