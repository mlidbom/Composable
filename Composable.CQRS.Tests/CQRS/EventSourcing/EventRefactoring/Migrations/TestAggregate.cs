using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using JetBrains.Annotations;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    interface IRootEvent : IAggregateRootEvent { }

    abstract class RootEvent : AggregateRootEvent, IRootEvent
    {}

    namespace Events
    {
        abstract class EcAbstract : RootEvent, IAggregateRootCreatedEvent
        {}

        // ReSharper disable ClassNeverInstantiated.Global
        internal class Ec1 : EcAbstract{}
        internal class Ec2 : EcAbstract{}
        internal class Ec3 : EcAbstract{}
        // ReSharper restore ClassNeverInstantiated.Global

        internal class E1 : RootEvent { }
        internal class E2 : RootEvent { }
        internal class E3 : RootEvent { }
        internal class E4 : RootEvent { }
        internal class E5 : RootEvent { }
        internal class E6 : RootEvent { }
        internal class E7 : RootEvent { }
        internal class E8 : RootEvent { }
        internal class E9 : RootEvent { }
        internal class Ef : RootEvent { }
    }


    class TestAggregate : AggregateRoot<TestAggregate, RootEvent, IRootEvent>
    {
        public void RaiseEvents(params RootEvent[] events)
        {
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateRootId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<AggregateRootEvent>().First().AggregateRootId = Id;
            }

            foreach (var @event in events)
            {
                RaiseEvent(@event);
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
            ContractOptimized.Argument(events.First(), "events.First()")
                             .IsOfType<IAggregateRootCreatedEvent>();

            RaiseEvents(events);
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