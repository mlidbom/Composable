using System;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Testing.Threading;
using FluentAssertions;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture : IDisposable
    {
        internal readonly ITestingEndpointHost Host;
        internal readonly IThreadGate CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate MyCreateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate MyUpdateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(5.Seconds());

        protected readonly TestingTaskRunner TaskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
        protected readonly IEndpoint DomainEndpoint;
        protected readonly IEndpoint ClientEndpoint;

        protected Fixture()
        {
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            DomainEndpoint = Host.RegisterAndStartEndpoint(
                "Backend",
                new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
                builder =>
                {
                    builder.Container.RegisterSqlServerEventStore("Backend")
                           .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>(builder.RegisterHandlers);

                    builder.RegisterHandlers
                           .ForCommand((MyExactlyOnceCommand command) => CommandHandlerThreadGate.AwaitPassthrough())
                           .ForCommand((MyCreateAggregateCommand command, ILocalApiNavigatorSession navigator) => MyCreateAggregateCommandHandlerThreadGate.AwaitPassthroughAndExecute(() => MyAggregate.Create(command.AggregateId, navigator)))
                           .ForCommand((MyUpdateAggregateCommand command, ILocalApiNavigatorSession navigator) => MyUpdateAggregateCommandHandlerThreadGate.AwaitPassthroughAndExecute(() => navigator.Execute(new ComposableApi().EventStore.Queries.GetForUpdate<MyAggregate>(command.AggregateId)).Update()))
                           .ForEvent((MyExactlyOnceEvent myEvent) => EventHandlerThreadGate.AwaitPassthrough())
                           .ForQuery((MyQuery query) => QueryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))
                           .ForCommandWithResult((MyAtMostOnceCommandWithResult command) => CommandHandlerWithResultThreadGate.AwaitPassthroughAndReturn(new MyCommandResult()));

                    builder.TypeMapper.Map<MyExactlyOnceCommand>("0ddefcaa-4d4d-48b2-9e1a-762c0b835275")
                           .Map<MyAtMostOnceCommandWithResult>("24248d03-630b-4909-a6ea-e7fdaf82baa2")
                           .Map<MyExactlyOnceEvent>("2fdde21f-c6d4-46a2-95e5-3429b820dfc3")
                           .Map<MyQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                           .Map<MyCreateAggregateCommand>("86bf04d8-8e6d-4e21-a95e-8af237f69f0f")
                           .Map<MyUpdateAggregateCommand>("c4ce3662-d068-4ec1-9c02-8d8f08640414");
                });

            ClientEndpoint = Host.ClientEndpoint;
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

        protected static class MyAggregateEvent
        {
            public interface IRoot : IAggregateEvent{}
            public interface Created : IRoot, IAggregateCreatedEvent {}
            public interface Updated : IRoot{}
            public class Implementation
            {
                public class Root : AggregateEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateId) : base(aggregateId) {}
                }

                // ReSharper disable once MemberHidesStaticFromOuterClass
                public class Created : Root, MyAggregateEvent.Created
                {
                    public Created(Guid aggregateId) : base(aggregateId) {}
                }

                // ReSharper disable once MemberHidesStaticFromOuterClass
                public class Updated : Root, MyAggregateEvent.Updated
                {
                }
            }
        }

        protected class MyAggregate : Aggregate<MyAggregate, MyAggregateEvent.Implementation.Root, MyAggregateEvent.IRoot>
        {
            public MyAggregate():base(new DateTimeNowTimeSource()) {}

            internal void Update() => Publish(new MyAggregateEvent.Implementation.Updated());

            internal static void Create(Guid id, ILocalApiNavigatorSession bus)
            {
                var created = new MyAggregate();
                created.Publish(new MyAggregateEvent.Implementation.Created(id));
                bus.Execute(new ComposableApi().EventStore.Commands.Save(created));
            }
        }

        protected class MyCreateAggregateCommand : BusApi.Remotable.ICommand
        {
            public Guid AggregateId { get; private set; } = Guid.NewGuid();
        }

        protected class MyUpdateAggregateCommand : BusApi.Remotable.ICommand
        {
            public MyUpdateAggregateCommand(Guid aggregateId) => AggregateId = aggregateId;
            public Guid AggregateId { get; private set; }
        }


        protected class MyExactlyOnceCommand : BusApi.Remotable.ExactlyOnce.Command {}
        protected class MyExactlyOnceEvent : AggregateEvent {}
        protected class MyQuery : BusApi.Remotable.NonTransactional.Queries.Query<MyQueryResult> {}
        protected class MyQueryResult {}
        protected class MyAtMostOnceCommand : BusApi.Remotable.AtMostOnce.Command<MyCommandResult> {}
        protected class MyAtMostOnceCommandWithResult : BusApi.Remotable.AtMostOnce.Command<MyCommandResult> {}
        protected class MyCommandResult {}
    }
}
