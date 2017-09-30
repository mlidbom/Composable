using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Paralellism_policies : _Fixture
    {
        [Fact] public void Command_handler_executes_on_different_thread_from_client_sending_command()
        {
            _host.ClientBus.Send(new MyCommand());

            _commandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
            _commandHandlerThreadGate.PassedThreads.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Event_handler_executes_on_different_thread_from_client_publishing_event()
        {
            _host.ClientBus.Publish(new MyEvent());

            _eventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
            _eventHandlerThreadGate.PassedThreads.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Query_handler_executes_on_different_thread_from_client_sending_query()
        {
            _host.ClientBus.Query(new MyQuery());

            _queryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThreads.Single().Should().NotBe(Thread.CurrentThread);
        }

        [Fact] public void Two_query_handlers_can_execute_in_parallel_when_using_QueryAsync()
        {
            CloseGates();
            _taskRunner.Monitor(_host.ClientBus.QueryAsync(new MyQuery()),
                                _host.ClientBus.QueryAsync(new MyQuery()));

            _queryHandlerThreadGate.AwaitQueueLengthEqualTo(2);
        }

        [Fact] public void Two_event_handlers_cannot_execute_in_parallel()
        {
            CloseGates();
            _host.ClientBus.Publish(new MyEvent());
            _host.ClientBus.Publish(new MyEvent());

            _eventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                   .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Two_command_handlers_cannot_execute_in_parallel()
        {
            CloseGates();
            _host.ClientBus.Send(new MyCommand());
            _host.ClientBus.Send(new MyCommand());

            _commandHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                     .TryAwaitQueueLengthEqualTo(2, timeout: 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public void Command_handler_cannot_execute_if_event_handler_is_executing()
        {
            CloseGates();

            _host.ClientBus.Publish(new MyEvent());
            _eventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            _host.ClientBus.Send(new MyCommand());

            _commandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public async Task Command_handler_with_result_cannot_execute_if_event_handler_is_executing()
        {
            CloseGates();
            _host.ClientBus.Publish(new MyEvent());
            _eventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            var result = _host.ClientBus.SendAsync(new MyCommandWithResult());
            _commandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);

            OpenGates();
            (await result).Should().NotBe(null);
        }

        [Fact] public void Event_handler_cannot_execute_if_command_handler_is_executing()
        {
            CloseGates();
            _host.ClientBus.Send(new MyCommand());
            _commandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            _host.ClientBus.Publish(new MyEvent());
            _eventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);
        }

        [Fact] public async Task Event_handler_cannot_execute_if_command_handler_with_result_is_executing()
        {
            CloseGates();

            var result = _host.ClientBus.SendAsync(new MyCommandWithResult());
            _commandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            _host.ClientBus.Publish(new MyEvent());
            _eventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds()).Should().Be(false);

            OpenGates();
            (await result).Should().NotBe(null);
        }
    }
}
