using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Messaging.Events;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    public class Given_a_backend_endpoint_and_a_client_endpoint : IDisposable
    {
        readonly ITestingEndpointHost _host;
        readonly IThreadGate _commandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        readonly IThreadGate _eventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        readonly IThreadGate _queryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

        public Given_a_backend_endpoint_and_a_client_endpoint()
        {
            _host = EndpointHost.Testing.BuildHost(
                buildHost => buildHost.RegisterAndStartEndpoint(
                    "Backend",
                    builder => builder.RegisterHandler
                                      .ForCommand((MyCommand command) => _commandHandlerThreadGate.AwaitPassthrough())
                                      .ForEvent((MyEvent myEvent) => _eventHandlerThreadGate.AwaitPassthrough())
                                      .ForQuery((MyQuery query) => _queryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))));
        }

        public void Dispose() { _host.Dispose(); }

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

        [Fact] public async Task Two_query_handler_can_execute_in_parallel_when_using_QueryAsync()
        {
            _queryHandlerThreadGate.Close();

            var queryTasks = Task.WhenAll(_host.ClientBus.QueryAsync(new MyQuery()),
                                          _host.ClientBus.QueryAsync(new MyQuery()));

            _queryHandlerThreadGate.AwaitQueueLengthEqualTo(2);
            _queryHandlerThreadGate.Open();

            await queryTasks;
        }

        [Fact] void Command_handler_runs_in_transaction()
        {
            _host.ClientBus.Send(new MyCommand());

            _commandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                     .PassedThreads.Single().Transaction.Should().NotBeNull();
        }

        [Fact]
        void Event_handler_runs_in_transaction()
        {
            _host.ClientBus.Publish(new MyEvent());

            _eventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                     .PassedThreads.Single().Transaction.Should().NotBeNull();
        }

        [Fact]
        void Query_handler_does_not_run_in_transaction()
        {
            _host.ClientBus.Query(new MyQuery());

            _queryHandlerThreadGate.AwaitPassedThroughCountEqualTo(1)
                                   .PassedThreads.Single().Transaction.Should().BeNull();
        }

        class MyCommand : Command {}
        class MyEvent : Event {}
        class MyQuery : IQuery<MyQueryResult> {}
        class MyQueryResult : IQueryResult {}
    }
}
