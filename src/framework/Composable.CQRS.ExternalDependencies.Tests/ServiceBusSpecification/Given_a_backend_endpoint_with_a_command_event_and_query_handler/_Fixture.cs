using System;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Testing.Threading;
using FluentAssertions;

// ReSharper disable InconsistentNaming

namespace Composable.CQRS.Tests.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class _Fixture : IDisposable
    {
        internal readonly ITestingEndpointHost _host;
        internal readonly IThreadGate _commandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate _eventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate _queryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

        protected readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());

        protected _Fixture()
        {
            _host = EndpointHost.Testing.BuildHost(
                buildHost => buildHost.RegisterAndStartEndpoint(
                    "Backend",
                    builder => builder.RegisterHandlers
                                      .ForCommand((MyCommand command, IServiceBus bus) => _commandHandlerThreadGate.AwaitPassthrough())
                                      .ForEvent((MyEvent myEvent) => _eventHandlerThreadGate.AwaitPassthrough())
                                      .ForQuery((MyQuery query) => _queryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))
                                      .ForCommand((MyCommandWithResult command) => _commandHandlerThreadGate.AwaitPassthroughAndReturn(new MyCommandResult()))));
        }

        public void Dispose()
        {
            OpenGates();

            _taskRunner.Dispose();
            _host.Dispose();
        }

        protected void CloseGates()
        {
            _eventHandlerThreadGate.Close();
            _commandHandlerThreadGate.Close();
            _queryHandlerThreadGate.Close();
        }

        protected void OpenGates()
        {
            _eventHandlerThreadGate.Open();
            _commandHandlerThreadGate.Open();
            _queryHandlerThreadGate.Open();
        }

        protected class MyCommand : Command {}
        protected class MyEvent : Event {}
        protected class MyQuery : Query<MyQueryResult> {}
        protected class MyQueryResult : QueryResult {}
        protected class MyCommandWithResult : Command<MyCommandResult> {}
        protected class MyCommandResult : Message {}
    }
}
