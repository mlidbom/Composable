using System;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
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

        [TypeId("C0616624-77D3-4082-A3C1-408A89D9C8AA")]protected class MyCommand : TransactionalExactlyOnceDeliveryCommand {}
        [TypeId("20B798AF-F7D7-448A-9F78-A189D4D5499A")]protected class MyEvent : AggregateRootEvent {}
        [TypeId("5D0DBBDE-5FF6-4771-9505-289922BE4333")]protected class MyQuery : Query<MyQueryResult> {}
        protected class MyQueryResult : QueryResult {}
        [TypeId("7CA55A90-436C-4DE1-98CF-1FB4328DE61F")]protected class MyCommandWithResult : TransactionalExactlyOnceDeliveryCommand<MyCommandResult> {}
        protected class MyCommandResult : Message {}
    }
}
