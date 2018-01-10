using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using JetBrains.Annotations;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TypeId("4DE32D33-C069-4573-8BF8-9AC9384E6679")]interface IRootEvent : IAggregateRootEvent { }

    abstract class RootEvent : AggregateRootEvent, IRootEvent
    {}

    namespace Events
    {
        abstract class EcAbstract : RootEvent, IAggregateRootCreatedEvent
        {}

        // ReSharper disable ClassNeverInstantiated.Global
        [TypeId("A13EC990-34E6-4518-94E7-19683635828A")]class Ec1 : EcAbstract{}
        [TypeId("7960759F-528A-446E-B06F-4AA35442207B")]class Ec2 : EcAbstract{}
        [TypeId("9F9F7CD4-0A46-4ABE-A546-68A1748AFAFC")]class Ec3 : EcAbstract{}
        // ReSharper restore ClassNeverInstantiated.Global

        [TypeId("048C01F3-8ECE-4748-88B6-1B128A1DB245")]class E1 : RootEvent { }
        [TypeId("8F87BBC7-D3C7-467B-8316-2F4E49EEC044")]class E2 : RootEvent { }
        [TypeId("CB1F3AAB-B032-415F-BDB1-C39E2D8C846B")]class E3 : RootEvent { }
        [TypeId("8EF297B0-4790-4918-94E3-2C8B1A8E65DD")]class E4 : RootEvent { }
        [TypeId("F802A5D2-3580-4572-BE23-4266842F20DF")]class E5 : RootEvent { }
        [TypeId("90CF0DAC-0F05-44F3-92C9-C8F620E5A73D")]class E6 : RootEvent { }
        [TypeId("4CC8F514-421B-4A6F-B84D-48D3731670F5")]class E7 : RootEvent { }
        [TypeId("D4F35154-7F6D-4046-838E-3388526BCFBE")]class E8 : RootEvent { }
        [TypeId("63EBE6EE-A702-4D28-A907-D4C5154B7F9C")]class E9 : RootEvent { }
        [TypeId("2ED04FE4-F0D5-423A-AC90-A18A88C56A16")]class Ef : RootEvent { }
    }


    class TestAggregate : AggregateRoot<TestAggregate, RootEvent, IRootEvent>
    {
        public void Publish(params RootEvent[] events)
        {
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateRootId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<AggregateRootEvent>().First().AggregateRootId = Id;
            }

            foreach (var @event in events)
            {
                base.Publish(@event);
            }
        }


        [Obsolete("For serialization only", error: true), UsedImplicitly]
        public TestAggregate()
        {
            SetupAppliers();
        }

        TestAggregate(IUtcTimeTimeSource timeSource):base(timeSource)
        {
            SetupAppliers();
        }

        void SetupAppliers()
        {
            RegisterEventAppliers()
                .For<IRootEvent>(e => _history.Add(e));
        }

        public TestAggregate(IUtcTimeTimeSource timeSource, params RootEvent[] events):this(timeSource)
        {
            OldContract.Assert.That(events.First() is IAggregateRootCreatedEvent, "events.First() is IAggregateRootCreatedEvent");

            Publish(events);
        }

        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> events)
        {
            var rootEvents = events.ToEvents();
            rootEvents.Cast<AggregateRootEvent>().First().AggregateRootId = id ?? Guid.NewGuid();
            return new TestAggregate(timeSource, rootEvents);
        }

        readonly List<IRootEvent> _history = new List<IRootEvent>();
        public IReadOnlyList<IAggregateRootEvent> History => _history;
    }

    static class EventSequenceGenerator
    {
        public static RootEvent[] ToEvents(this IEnumerable<Type> types) => types.Select(Activator.CreateInstance).Cast<RootEvent>().ToArray();
    }
}