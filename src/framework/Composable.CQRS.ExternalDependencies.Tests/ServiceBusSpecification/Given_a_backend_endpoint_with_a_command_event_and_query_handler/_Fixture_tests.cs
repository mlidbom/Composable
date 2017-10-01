using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture_tests : Fixture
    {
        [Fact] public void Does_not_hang_if_command_handler_fails()
        {
            CommandHandlerThreadGate.SetPassThroughAction(_ => throw new IntentionalException());
            Host.ClientBus.Send(new MyCommand());
            AssertThrowsAggregateExceptionWithSingleInnerExceptionOfType_IntentionalException(TestDispose);
        }

        [Fact] public async Task Does_not_hang_if_command_handler_with_result_fails()
        {
            CommandHandlerWithResultThreadGate.SetPassThroughAction(_ => throw new IntentionalException());
            var exceptionResult = Host.ClientBus.SendAsync(new MyCommandWithResult());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionOfType_IntentionalException(TestDispose);

            await Assert.ThrowsAsync<IntentionalException>(async () => await exceptionResult);
        }

        [Fact] public void Does_not_hang_if_event_handler_fails()
        {
            EventHandlerThreadGate.SetPassThroughAction(_ => throw new IntentionalException());
            Host.ClientBus.Publish(new MyEvent());
            AssertThrowsAggregateExceptionWithSingleInnerExceptionOfType_IntentionalException(TestDispose);
        }

        [Fact] public async Task Does_not_hang_if_QueryAsync_fails()
        {
            QueryHandlerThreadGate.SetPassThroughAction(_ => throw new IntentionalException());
            var queryResult = Host.ClientBus.QueryAsync(new MyQuery());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionOfType_IntentionalException(TestDispose);

            await Assert.ThrowsAsync<IntentionalException>(async () => await queryResult);
        }

        [Fact] public void Does_not_hang_if_query_fails()
        {
            QueryHandlerThreadGate.SetPassThroughAction(_ => throw new IntentionalException());

            AssertThrowsAggregateExceptionWithSingleInnerExceptionOfType_IntentionalException(
                () => Host.ClientBus.Query(new MyQuery()),
                TestDispose);
        }

        static void AssertThrowsAggregateExceptionWithSingleInnerExceptionOfType_IntentionalException(params Action[] actions)
        {
            foreach(var action in actions)
            {
                Assert.Throws<AggregateException>(action)
                      .InnerExceptions.Single().Should().BeOfType<IntentionalException>();
            }
        }

        class IntentionalException : Exception {}
    }
}
