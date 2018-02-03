using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Reflection;
using JetBrains.Annotations;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    interface IRootEvent : IAggregateEvent { }

    abstract class RootEvent : AggregateEvent, IRootEvent
    {}

    namespace Events
    {
        abstract class EcAbstract : RootEvent, IAggregateCreatedEvent
        {}

        // ReSharper disable ClassNeverInstantiated.Global
        class Ec1 : EcAbstract{}
        class Ec2 : EcAbstract{}
        class Ec3 : EcAbstract{}
        // ReSharper restore ClassNeverInstantiated.Global

        class E1 : RootEvent { }
        class E2 : RootEvent { }
        class E3 : RootEvent { }
        class E4 : RootEvent { }
        class E5 : RootEvent { }
        class E6 : RootEvent { }
        class E7 : RootEvent { }
        class E8 : RootEvent { }
        class E9 : RootEvent { }
        class Ef : RootEvent { }
    }


    class TestAggregate : Aggregate<TestAggregate, RootEvent, IRootEvent>
    {
        public void Publish(params RootEvent[] events)
        {
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<AggregateEvent>().First().AggregateId = Id;
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
            Contract.Assert.That(events.First() is IAggregateCreatedEvent, "events.First() is IAggregateCreatedEvent");

            Publish(events);
        }

        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> events)
        {
            var rootEvents = events.ToEvents();
            rootEvents.Cast<AggregateEvent>().First().AggregateId = id ?? Guid.NewGuid();
            return new TestAggregate(timeSource, rootEvents);
        }

        readonly List<IRootEvent> _history = new List<IRootEvent>();
        public IReadOnlyList<IAggregateEvent> History => _history;
    }

    static class EventSequenceGenerator
    {
        public static RootEvent[] ToEvents(this IEnumerable<Type> types) => types.Select(Constructor.CreateInstance).Cast<RootEvent>().ToArray();
    }
}