using System;
using System.Collections.Generic;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Testing.Threading;
using FluentAssertions;

// ReSharper disable InconsistentNaming

namespace Composable.CQRS.Tests.ServiceBusSpecification
{
    public partial class Given_a_backend_endpoint_with_command_event_and_query_handlers : IDisposable
    {
        readonly ITestingEndpointHost _host;
        readonly IThreadGate _commandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        readonly IThreadGate _eventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        readonly IThreadGate _queryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

        readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());

        protected Given_a_backend_endpoint_with_command_event_and_query_handlers()
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

        void CloseGates()
        {
            _eventHandlerThreadGate.Close();
            _commandHandlerThreadGate.Close();
            _queryHandlerThreadGate.Close();
        }

        void OpenGates()
        {
            _eventHandlerThreadGate.Open();
            _commandHandlerThreadGate.Open();
            _queryHandlerThreadGate.Open();
        }

        class MyCommand : Command {}
        class MyEvent : Event {}
        class MyQuery : Query<MyQueryResult> {}
        class MyQueryResult : QueryResult {}
        class MyCommandWithResult : Command<MyCommandResult> {}
        class MyCommandResult : Message {}
    }
}
