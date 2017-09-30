using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;


namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Paralellism_policies : Fixture
    {
        [Fact] public void Command_handler_executes_on_different_thread_from_client_sending_command()
        {
            Host.ClientBus.Send(new MyCommand());

            CommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
            CommandHandlerThreadGate.PassedThreads.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Event_handler_executes_on_different_thread_from_client_publishing_event()
        {
            Host.ClientBus.Publish(new MyEvent());

            EventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
            EventHandlerThreadGate.PassedThreads.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Query_handler_executes_on_different_thread_from_client_sending_query()
        {
            Host.ClientBus.Query(new MyQuery());

            QueryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThreads.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Five_query_handlers_can_execute_in_parallel_when_using_QueryAsync()
        {
            CloseGates();
            TaskRunner.RunTimes(5, () => Host.ClientBus.QueryAsync(new MyQuery()));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
        }

        [Fact]
        public void Five_query_handlers_can_execute_in_parallel_when_using_Query()
        {
            CloseGates();
            TaskRunner.RunTimes(5, () => Host.ClientBus.Query(new MyQuery()));

            QueryHandlerThreadGate.AwaitQueueLengthEqualTo(5);
        }


        [Fact] public void Two_event_handlers_cannot_execute_in_parallel()
        {
            CloseGates();
            Host.ClientBus.Publish(new MyEvent());
            Host.ClientBus.Publish(new MyEvent());

            EventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                   .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Two_command_handlers_cannot_execute_in_parallel()
        {
            CloseGates();
            Host.ClientBus.Send(new MyCommand());
            Host.ClientBus.Send(new MyCommand());

            CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                     .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Command_handler_cannot_execute_if_event_handler_is_executing()
        {
            CloseGates();

            Host.ClientBus.Publish(new MyEvent());
            EventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            Host.ClientBus.Send(new MyCommand());

            CommandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public async Task Command_handler_with_result_cannot_execute_if_event_handler_is_executing()
        {
            CloseGates();
            Host.ClientBus.Publish(new MyEvent());
            EventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            var result = Host.ClientBus.SendAsync(new MyCommandWithResult());
            CommandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);

            OpenGates();
            (await result).Should().NotBe(null);
        }

        [Fact] public void Event_handler_cannot_execute_if_command_handler_is_executing()
        {
            CloseGates();
            Host.ClientBus.Send(new MyCommand());
            CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            Host.ClientBus.Publish(new MyEvent());
            EventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public async Task Event_handler_cannot_execute_if_command_handler_with_result_is_executing()
        {
            CloseGates();

            var result = Host.ClientBus.SendAsync(new MyCommandWithResult());
            CommandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            Host.ClientBus.Publish(new MyEvent());
            EventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);

            OpenGates();
            (await result).Should().NotBe(null);
        }
    }
}
