using System;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Testing.Threading;
using FluentAssertions;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture : IDisposable
    {
        internal readonly ITestingEndpointHost Host;
        internal readonly IThreadGate CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());

        protected readonly TestingTaskRunner TaskRunner = TestingTaskRunner.WithTimeout(1.Seconds());

        protected Fixture()
        {
            Host = EndpointHost.Testing.BuildHost(
                DependencyInjectionContainer.Create,
                buildHost => buildHost.RegisterAndStartEndpoint(
                    "Backend",
                    builder => builder.RegisterHandlers
                                      .ForCommand((MyCommand command) => CommandHandlerThreadGate.AwaitPassthrough())
                                      .ForEvent((MyEvent myEvent) => EventHandlerThreadGate.AwaitPassthrough())
                                      .ForQuery((MyQuery query) => QueryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))
                                      .ForCommandWithResult((MyCommandWithResult command) => CommandHandlerWithResultThreadGate.AwaitPassthroughAndReturn(new MyCommandResult()))));
        }

        public virtual void Dispose()
        {
            OpenGates();
            TaskRunner.Dispose();
            Host.Dispose();
        }

        protected void CloseGates()
        {
            EventHandlerThreadGate.Close();
            CommandHandlerThreadGate.Close();
            CommandHandlerWithResultThreadGate.Close();
            QueryHandlerThreadGate.Close();
        }

        protected void OpenGates()
        {
            EventHandlerThreadGate.Open();
            CommandHandlerThreadGate.Open();
            CommandHandlerWithResultThreadGate.Open();
            QueryHandlerThreadGate.Open();
        }

        protected class MyCommand : DomainCommand {}
        protected class MyEvent : Event {}
        protected class MyQuery : Query<MyQueryResult> {}
        protected class MyQueryResult : QueryResult {}
        protected class MyCommandWithResult : DomainCommand<MyCommandResult> {}
        protected class MyCommandResult : Message {}
    }
}
