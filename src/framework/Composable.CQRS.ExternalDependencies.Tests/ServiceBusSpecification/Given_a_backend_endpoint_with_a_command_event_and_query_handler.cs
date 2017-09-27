using System;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Messaging.Events;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    public class Given_a_backend_endpoint_with_a_command_event_and_query_handler : IDisposable
    {
        readonly ITestingEndpointHost _host;
        readonly IThreadGate _commandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        readonly IThreadGate _eventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        readonly IThreadGate _queryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

        readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());

        public Given_a_backend_endpoint_with_a_command_event_and_query_handler()
        {
            _host = EndpointHost.Testing.BuildHost(
                buildHost => buildHost.RegisterAndStartEndpoint(
                    "Backend",
                    builder => builder.RegisterHandler
                                      .ForCommand((MyCommand command) => _commandHandlerThreadGate.AwaitPassthrough())
                                      .ForEvent((MyEvent myEvent) => _eventHandlerThreadGate.AwaitPassthrough())
                                      .ForQuery((MyQuery query) => _queryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))));
        }

        public void Dispose()
        {
            _commandHandlerThreadGate.Open();
            _eventHandlerThreadGate.Open();
            _queryHandlerThreadGate.Open();

            _taskRunner.Dispose();
            _host.Dispose();
        }

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

        [Fact] public void Two_query_handler_can_execute_in_parallel_when_using_QueryAsync()
        {
            _queryHandlerThreadGate.Close();

            _taskRunner.Monitor(_host.ClientBus.QueryAsync(new MyQuery()),
                                _host.ClientBus.QueryAsync(new MyQuery()));

            _queryHandlerThreadGate.AwaitQueueLengthEqualTo(2);
            _queryHandlerThreadGate.Open();
        }

        [Fact] public void Two_event_handlers_cannot_execute_in_parallel()
        {
            _eventHandlerThreadGate.Close();

            _host.ClientBus.Publish(new MyEvent());
            _host.ClientBus.Publish(new MyEvent());

            _eventHandlerThreadGate.AwaitQueueLengthEqualTo(1)
                                   .TryAwaitQueueLengthEqualTo(2, 100.Milliseconds())
                                   .Should().Be(false);
        }

        [Fact] public void Two_command_handlers_cannot_execute_in_parallel()
        {
            _commandHandlerThreadGate.Close();

            _host.ClientBus.Send(new MyCommand());
            _host.ClientBus.Send(new MyCommand());

            _commandHandlerThreadGate.AwaitQueueLengthEqualTo(1).TryAwaitQueueLengthEqualTo(2, 100.Milliseconds())
                                     .Should().Be(false);
        }

        [Fact] public void Command_handler_cannot_execute_if_event_handler_is_executing()
        {
            _commandHandlerThreadGate.Close();
            _eventHandlerThreadGate.Close();

            _host.ClientBus.Publish(new MyEvent());
            _eventHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            _host.ClientBus.Send(new MyCommand());

            _commandHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds())
                                     .Should().Be(false);
        }

        [Fact] public void Event_handler_cannot_execute_if_command_handler_is_executing()
        {
            _commandHandlerThreadGate.Close();
            _eventHandlerThreadGate.Close();

            _host.ClientBus.Send(new MyCommand());
            _commandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

            _host.ClientBus.Publish(new MyEvent());

            _eventHandlerThreadGate.TryAwaitQueueLengthEqualTo(1, 100.Milliseconds())
                                   .Should().BeFalse();
        }

        [Fact] void Command_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            _host.ClientBus.Send(new MyCommand());

            var transaction = _commandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                       .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Event_handler_runs_in_transaction_with_isolation_level_Serializable()
        {
            _host.ClientBus.Publish(new MyEvent());

            var transaction = _eventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                                     .PassedThreads.Single().Transaction;
            transaction.Should().NotBeNull();
            transaction.IsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact] void Query_handler_does_not_run_in_transaction()
        {
            _host.ClientBus.Query(new MyQuery());

            _queryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThreads.Single().Transaction.Should().BeNull();
        }

        class MyCommand : Command {}
        class MyEvent : Event {}
        class MyQuery : Query<MyQueryResult> {}
        class MyQueryResult : QueryResult {}
    }
}
