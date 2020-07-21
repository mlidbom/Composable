using Composable.Messaging.Buses;
using Composable.Testing.Threading;
using FluentAssertions;
using NUnit.Framework;
using Composable.System;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Parallelism_policies : Fixture
    {
        [Test] public void Five_query_handlers_can_execute_in_parallel_when_using_QueryAsync()
        {
            CloseGates();
            TaskRunner.StartTimes(5, () => ClientEndpoint.ExecuteClientRequestAsync(session => session.GetAsync(new MyQuery())));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
        }

        [Test] public void Five_query_handlers_can_execute_in_parallel_when_using_Query()
        {
            CloseGates();
            TaskRunner.StartTimes(5, () => ClientEndpoint.ExecuteClientRequest(session => session.Get(new MyQuery())));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
        }

        [Test] public void Two_event_handlers_cannot_execute_in_parallel()
        {
            MyRemoteAggregateEventHandlerThreadGate.Close();
            ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));
            ClientEndpoint.ExecuteClientRequest(session => session.Post(MyCreateAggregateCommand.Create()));

            MyRemoteAggregateEventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                  .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Test] public void Two_exactly_once_command_handlers_cannot_execute_in_parallel()
        {
            CloseGates();

            RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));
            RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

            CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                    .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Test] public void Two_AtMostOnce_command_handlers_from_the_same_session_cannot_execute_in_parallel()
        {
            CloseGates();

            ClientEndpoint.ExecuteClientRequest(session =>
            {
               session.PostAsync(MyCreateAggregateCommand.Create());
                session.PostAsync(MyCreateAggregateCommand.Create());
            });

            MyCreateAggregateCommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                    .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        public Parallelism_policies(string _) : base(_) {}
    }
}
