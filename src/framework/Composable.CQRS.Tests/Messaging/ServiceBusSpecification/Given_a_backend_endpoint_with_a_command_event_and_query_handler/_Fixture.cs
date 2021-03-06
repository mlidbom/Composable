﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Hypermedia;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.Testing.Threading;
using JetBrains.Annotations;
using NUnit.Framework;
using Composable.Testing;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming for testing

#pragma warning disable IDE1006 //Review OK: Test Naming Styles
#pragma warning disable CA1724  // Type names should not match namespaces

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture : DuplicateByPluggableComponentTest
    {
        static readonly TimeSpan _timeout = 10.Seconds();
        internal ITestingEndpointHost Host;
        internal IThreadGate CommandHandlerThreadGate;
        internal IThreadGate CommandHandlerWithResultThreadGate;
        internal IThreadGate MyCreateAggregateCommandHandlerThreadGate;
        internal IThreadGate MyUpdateAggregateCommandHandlerThreadGate;
        internal IThreadGate MyRemoteAggregateEventHandlerThreadGate;
        internal IThreadGate MyLocalAggregateEventHandlerThreadGate;
        internal IThreadGate EventHandlerThreadGate;
        internal IThreadGate QueryHandlerThreadGate;

        internal IReadOnlyList<IThreadGate> AllGates;

        protected TestingTaskRunner TaskRunner { get; } = TestingTaskRunner.WithTimeout(_timeout);
        protected IEndpoint ClientEndpoint { get; set; }
        protected IEndpoint RemoteEndpoint { get; set; }
        protected IRemoteHypermediaNavigator RemoteNavigator => ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();

        [SetUp]public async Task Setup()
        {
            static void MapBackendEndpointTypes(IEndpointBuilder builder) =>
                builder.TypeMapper.Map<MyExactlyOnceCommand>("0ddefcaa-4d4d-48b2-9e1a-762c0b835275")
                       .Map<MyAtMostOnceCommandWithResult>("24248d03-630b-4909-a6ea-e7fdaf82baa2")
                       .Map<MyExactlyOnceEvent>("2fdde21f-c6d4-46a2-95e5-3429b820dfc3")
                       .Map<IMyExactlyOnceEvent>("49ba71a4-5f4c-4930-9e01-62bc0551d8c8")
                       .Map<MyQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                       .Map<MyCreateAggregateCommand>("86bf04d8-8e6d-4e21-a95e-8af237f69f0f")
                       .Map<MyUpdateAggregateCommand>("c4ce3662-d068-4ec1-9c02-8d8f08640414")
                       .Map<MyAggregateEvent.IRoot>("8b19a261-b74b-4c05-91e3-d062dc879635")
                       .Map<MyAggregate>("8b7df016-3763-4033-8240-f46fa836ebfb")
                       .Map<MyAggregateEvent.Created>("41f96e37-657f-464a-a4d1-004eba4e8e7b")
                       .Map<MyAggregateEvent.Implementation.Created>("0ea2f548-0d24-4bb0-a59a-820bc35f3935")
                       .Map<MyAggregateEvent.Implementation.Root>("5a792961-3fbc-4d50-b06e-77fc35cb6edf")
                       .Map<MyAggregateEvent.Implementation.Updated>("bead75b3-9ecf-4f6b-b8c6-973a02168256")
                       .Map<MyAggregateEvent.Updated>("2a8b19f0-20df-480d-b120-71ed5151b174")
                       .Map<MyCommandResult>("4b2f17d2-2997-4532-9296-689495ed6958")
                       .Map<MyQueryResult>("9f3c69f0-0886-483c-a726-b79fb1c56120");

            Host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);

            Host.RegisterEndpoint(
                "Backend",
                new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
                builder =>
                {
                    builder.RegisterCurrentTestsConfiguredPersistenceLayer();
                    builder.RegisterEventStore()
                           .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>();

                    builder.RegisterHandlers
                           .ForCommand((MyExactlyOnceCommand command) => CommandHandlerThreadGate.AwaitPassThrough())
                           .ForCommand((MyCreateAggregateCommand command, ILocalHypermediaNavigator navigator) => MyCreateAggregateCommandHandlerThreadGate.AwaitPassthroughAndExecute(() => MyAggregate.Create(command.AggregateId, navigator)))
                           .ForCommand((MyUpdateAggregateCommand command, ILocalHypermediaNavigator navigator) => MyUpdateAggregateCommandHandlerThreadGate.AwaitPassthroughAndExecute(() => navigator.Execute(new ComposableApi().EventStore.Queries.GetForUpdate<MyAggregate>(command.AggregateId)).Update()))
                           .ForEvent((IMyExactlyOnceEvent myEvent) => EventHandlerThreadGate.AwaitPassThrough())
                           .ForEvent((MyAggregateEvent.IRoot myAggregateEvent) => MyLocalAggregateEventHandlerThreadGate.AwaitPassThrough())
                           .ForQuery((MyQuery query) => QueryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))
                           .ForCommandWithResult((MyAtMostOnceCommandWithResult command) => CommandHandlerWithResultThreadGate.AwaitPassthroughAndReturn(new MyCommandResult()));

                    MapBackendEndpointTypes(builder);
                });

            RemoteEndpoint = Host.RegisterEndpoint("Remote",
                                  new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                  builder =>
                                  {
                                      builder.RegisterCurrentTestsConfiguredPersistenceLayer();
                                      builder.RegisterHandlers.ForEvent((MyAggregateEvent.IRoot myAggregateEvent) => MyRemoteAggregateEventHandlerThreadGate.AwaitPassThrough());
                                      MapBackendEndpointTypes(builder);
                                  });

            ClientEndpoint = Host.RegisterClientEndpointForRegisteredEndpoints();

            await Host.StartAsync();
            AllGates = new List<IThreadGate>
                       {
                           (CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (MyCreateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (MyUpdateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (MyRemoteAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (MyLocalAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout)),
                           (QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(_timeout))
                       };
        }

        [TearDown]public virtual void TearDown()
        {
            OpenGates();
            TaskRunner.Dispose();
            Host.Dispose();
        }

        protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

        protected void OpenGates() => AllGates?.ForEach(gate => gate.Open());

        protected static class MyAggregateEvent
        {
            public interface IRoot : IAggregateEvent {}
            public interface Created : IRoot, IAggregateCreatedEvent {}
            public interface Updated : IRoot {}
            public static class Implementation
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
                public class Updated : Root, MyAggregateEvent.Updated {}
            }
        }

        protected class MyAggregate : Aggregate<MyAggregate, MyAggregateEvent.Implementation.Root, MyAggregateEvent.IRoot>
        {
            public MyAggregate() : base(new DateTimeNowTimeSource())
            {
                RegisterEventAppliers()
                   .IgnoreUnhandled<MyAggregateEvent.IRoot>();
            }

            internal void Update() => Publish(new MyAggregateEvent.Implementation.Updated());

            internal static void Create(Guid id, ILocalHypermediaNavigator bus)
            {
                var created = new MyAggregate();
                created.Publish(new MyAggregateEvent.Implementation.Created(id));
                bus.Execute(new ComposableApi().EventStore.Commands.Save(created));
            }
        }

        protected class MyCreateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
        {
            MyCreateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}

            internal static MyCreateAggregateCommand Create() => new MyCreateAggregateCommand
                                                                 {
                                                                     MessageId = Guid.NewGuid(),
                                                                     AggregateId = Guid.NewGuid()
                                                                 };

            public Guid AggregateId { get; set; }
        }

        protected class MyUpdateAggregateCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand
        {
            [UsedImplicitly] MyUpdateAggregateCommand() : base(DeduplicationIdHandling.Reuse) {}
            public MyUpdateAggregateCommand(Guid aggregateId) : base(DeduplicationIdHandling.Create) => AggregateId = aggregateId;
            public Guid AggregateId { get; private set; }
        }

        protected class MyExactlyOnceCommand : MessageTypes.Remotable.ExactlyOnce.Command {}

        protected interface IMyExactlyOnceEvent : IAggregateEvent {}
        protected class MyExactlyOnceEvent : AggregateEvent, IMyExactlyOnceEvent {}
        protected class MyQuery : MessageTypes.Remotable.NonTransactional.Queries.Query<MyQueryResult> {}
        protected class MyQueryResult {}
        protected class MyAtMostOnceCommand : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<MyCommandResult>
        {
            protected MyAtMostOnceCommand() : base(DeduplicationIdHandling.Reuse) {}
            internal static MyAtMostOnceCommand Create() => new MyAtMostOnceCommand {MessageId = Guid.NewGuid()};
        }

        protected class MyAtMostOnceCommandWithResult : MessageTypes.Remotable.AtMostOnce.AtMostOnceCommand<MyCommandResult>
        {
            MyAtMostOnceCommandWithResult() : base(DeduplicationIdHandling.Reuse) {}
            internal static MyAtMostOnceCommandWithResult Create() => new MyAtMostOnceCommandWithResult {MessageId = Guid.NewGuid()};
        }
        protected class MyCommandResult {}

        public Fixture(string _) : base(_) {}
    }
}
