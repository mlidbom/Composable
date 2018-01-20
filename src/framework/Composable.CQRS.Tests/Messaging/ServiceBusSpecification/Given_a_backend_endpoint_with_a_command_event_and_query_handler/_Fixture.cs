using System;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
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
        internal readonly IThreadGate QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(5.Seconds());

        protected readonly TestingTaskRunner TaskRunner = TestingTaskRunner.WithTimeout(1.Seconds());

        protected Fixture()
        {
            Host = EndpointHost.Testing.BuildHost(
                DependencyInjectionContainer.Create,
                buildHost => buildHost.RegisterAndStartEndpoint(
                    "Backend",
                    new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
                    builder =>
                    {
                        builder.RegisterHandlers
                               .ForCommand((MyCommand command) => CommandHandlerThreadGate.AwaitPassthrough())
                               .ForEvent((MyEvent myEvent) => EventHandlerThreadGate.AwaitPassthrough())
                               .ForQuery((MyQuery query) => QueryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))
                               .ForCommandWithResult((MyCommandWithResult command) => CommandHandlerWithResultThreadGate.AwaitPassthroughAndReturn(new MyCommandResult()));

                        builder.TypeMapper.Map<MyCommand>("0ddefcaa-4d4d-48b2-9e1a-762c0b835275")
                               .Map<MyCommandWithResult>("24248d03-630b-4909-a6ea-e7fdaf82baa2")
                               .Map<MyEvent>("2fdde21f-c6d4-46a2-95e5-3429b820dfc3")
                               .Map<MyQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144");
                    }));
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

        protected class MyCommand : TransactionalExactlyOnceDeliveryCommand {}
        protected class MyEvent : AggregateRootEvent {}
        protected class MyQuery : Query<MyQueryResult> {}
        protected class MyQueryResult : QueryResult {}
        protected class MyCommandWithResult : TransactionalExactlyOnceDeliveryCommand<MyCommandResult> {}
        protected class MyCommandResult : Message {}
    }
}
