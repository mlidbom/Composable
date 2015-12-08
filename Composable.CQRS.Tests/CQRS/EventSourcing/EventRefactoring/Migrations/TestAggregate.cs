using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.System.Linq;
using JetBrains.Annotations;

namespace TestAggregates
{
    public interface IRootEvent : IAggregateRootEvent { }

    public abstract class RootEvent : AggregateRootEvent, IRootEvent
    {
        protected RootEvent() { }
        protected RootEvent(Guid aggregateRootId) : base(aggregateRootId) { }
    }

    namespace Events
    {
        public abstract class ECAbstract : RootEvent, IAggregateRootCreatedEvent
        {
            public ECAbstract() : this(Guid.NewGuid()) { }
            public ECAbstract(Guid aggregateRootId) : base(aggregateRootId) { }
        }

        public class Ec1 : ECAbstract
        {
            public Ec1() { }
            public Ec1(Guid aggregateRootId) : base(aggregateRootId) { }
        }

        public class Ec2 : ECAbstract
        {
            public Ec2() { }
            public Ec2(Guid aggregateRootId) : base(aggregateRootId) { }
        }

        public class Ec3 : ECAbstract
        {
            public Ec3() { }
            public Ec3(Guid aggregateRootId) : base(aggregateRootId) { }
        }

        public class E1 : RootEvent { }
        public class E2 : RootEvent { }
        public class E3 : RootEvent { }
        public class E4 : RootEvent { }
        public class E5 : RootEvent { }
        public class E6 : RootEvent { }
        public class E7 : RootEvent { }
        public class E8 : RootEvent { }
        public class E9 : RootEvent { }
        public class Ef : RootEvent { }
    }


    public class TestAggregate : AggregateRoot<TestAggregate, RootEvent, IRootEvent>
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

        private TestAggregate(IUtcTimeTimeSource timeSource):base(timeSource)
        {
            SetupAppliers();
        }

        private void SetupAppliers()
        {
            RegisterEventAppliers()
                .For<IRootEvent>(e => _history.Add(e));
        }

        public TestAggregate(IUtcTimeTimeSource timeSource, params RootEvent[] events):this(timeSource)
        {
            Contract.Requires(events.First() is IAggregateRootCreatedEvent);

            RaiseEvents(events);
        }

        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, params RootEvent[] events) { return new TestAggregate(timeSource, events); }
        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> events)
        {
            var rootEvents = events.ToEvents();
            rootEvents.Cast<AggregateRootEvent>().First().AggregateRootId = id ?? Guid.NewGuid();
            return new TestAggregate(timeSource, rootEvents);
        }

        public static TestAggregate FromEvents<T1>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1>()); }
        public static TestAggregate FromEvents<T1, T2>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2>()); }
        public static TestAggregate FromEvents<T1, T2, T3>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(IUtcTimeTimeSource timeSource, Guid? id = null) { return FromEvents(timeSource, id, Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>()); }



        private readonly List<IRootEvent> _history = new List<IRootEvent>();
        public IReadOnlyList<IAggregateRootEvent> History => _history;
    }

    public static class EventSequenceGenerator
    {
        public static RootEvent[] ToEvents(this IEnumerable<Type> types)
        {
            return types.Select(Activator.CreateInstance).Cast<RootEvent>().ToArray();
        }
    }
}