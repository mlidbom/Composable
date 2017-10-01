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
        [Fact] public void If_command_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            CommandHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            Host.ClientBus.Send(new MyCommand());
            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(Host.Dispose);
        }

        [Fact] public async Task If_command_handler_with_result_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            CommandHandlerWithResultThreadGate.ThrowOnPassThrough(_thrownException);
            var exceptionResult = Host.ClientBus.SendAsync(new MyCommandWithResult());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(Host.Dispose);

            (await Assert.ThrowsAsync<IntentionalException>(async () => await exceptionResult)).Should().Be(_thrownException);
        }

        [Fact] public void If_event_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            EventHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            Host.ClientBus.Publish(new MyEvent());
            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(Host.Dispose);
        }

        [Fact] public async Task If_query_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(_thrownException);
            var queryResult = Host.ClientBus.QueryAsync(new MyQuery());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(Host.Dispose);

            (await Assert.ThrowsAsync<IntentionalException>(async () => await queryResult)).Should().Be(_thrownException);
        }

        [Fact] public void If_query_handler_throws_Query_and_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            QueryHandlerThreadGate.ThrowOnPassThrough(_thrownException);

            AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(
                () => Host.ClientBus.Query(new MyQuery()),
                Host.Dispose);
        }

        void AssertThrowsAggregateExceptionWithSingleInnerExceptionThatIsThrownException(params Action[] actions)
        {
            foreach(var action in actions)
            {
                Assert.Throws<AggregateException>(action)
                      .InnerExceptions.Single().Should().Be(_thrownException);
            }
        }

        readonly IntentionalException _thrownException = new IntentionalException();
        class IntentionalException : Exception {}
    }
}
