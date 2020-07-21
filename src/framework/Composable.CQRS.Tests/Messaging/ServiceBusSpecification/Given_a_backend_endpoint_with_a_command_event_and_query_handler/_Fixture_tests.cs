using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Testing;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture_tests : Fixture
    {
        [Test] public void If_command_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            CommandHandlerThreadGate.ThrowPostPassThrough(_thrownException);
            RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        [Test] public async Task If_command_handler_with_result_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
        {
            CommandHandlerWithResultThreadGate.ThrowPostPassThrough(_thrownException);
            await AssertThrows.Async<MessageDispatchingFailedException>(async () => await ClientEndpoint.ExecuteClientRequest(session => session.PostAsync(MyAtMostOnceCommandWithResult.Create())));

            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        [Test] public void If_event_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception()
        {
            MyRemoteAggregateEventHandlerThreadGate.ThrowPostPassThrough(_thrownException);
            ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));
            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        [Test] public async Task If_query_handler_throws_disposing_host_throws_AggregateException_containing_a_single_exception_that_is_the_thrown_exception_and_SendAsync_throws_MessageDispatchingFailedException()
        {
            QueryHandlerThreadGate.ThrowPostPassThrough(_thrownException);
            await AssertThrows.Async<MessageDispatchingFailedException>(() => ClientEndpoint.ExecuteClientRequest(session => session.GetAsync(new MyQuery())));

            AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException();
        }

        void AssertDisposingHostThrowsAggregateExceptionContainingOnlyThrownException()
        {
            AssertThrows.Exception<AggregateException>(Host.Dispose)
                  .InnerExceptions.Single().Should().Be(_thrownException);
        }

        readonly IntentionalException _thrownException = new IntentionalException();
        class IntentionalException : Exception {}

        public Fixture_tests(string _) : base(_) {}
    }
}
