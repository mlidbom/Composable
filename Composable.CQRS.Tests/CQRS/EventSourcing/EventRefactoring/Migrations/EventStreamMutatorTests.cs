using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;

namespace TestAggregates
{
    public interface IRootEvent : IAggregateRootEvent {}

    public abstract class RootEvent : AggregateRootEvent, IRootEvent
    {
        protected RootEvent() {}
        protected RootEvent(Guid aggregateRootId) : base(aggregateRootId) {}
    }

    namespace Events
    {
        public abstract class ECAbstract : RootEvent, IAggregateRootCreatedEvent
        {
            public ECAbstract():this(Guid.NewGuid()) {}
            public ECAbstract(Guid aggregateRootId) : base(aggregateRootId) {}
        }

        public class Ec1 : ECAbstract
        {
            public Ec1() {}
            public Ec1(Guid aggregateRootId) : base(aggregateRootId) {}
        }

        public class Ec2 : ECAbstract
        {
            public Ec2() {}
            public Ec2(Guid aggregateRootId) : base(aggregateRootId) {}
        }

        public class Ec3 : ECAbstract
        {
            public Ec3() {}
            public Ec3(Guid aggregateRootId) : base(aggregateRootId) {}
        }

        public class E1 : RootEvent {}
        public class E2 : RootEvent {}
        public class E3 : RootEvent {}
        public class E4 : RootEvent {}
        public class E5 : RootEvent {}
        public class E6 : RootEvent {}
        public class E7 : RootEvent {}
        public class E8 : RootEvent {}
        public class E9 : RootEvent {}
    }


    public class TestAggregate : AggregateRoot<TestAggregate, IRootEvent>
    {
        public void RaiseEvents(params IRootEvent[] events)
        {
            if(GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateRootId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.First().AggregateRootId = Id;
            }

            foreach(var @event in events)
            {
                RaiseEvent(@event);
            }
        }

        public TestAggregate(params IRootEvent[] events)
        {
            Contract.Requires(events.First() is IAggregateRootCreatedEvent);

            RegisterEventAppliers()
                .For<IRootEvent>(e => _history.Add(e));

            RaiseEvents(events);
        }

        public static TestAggregate FromEvents(params IRootEvent[] events) { return new TestAggregate(events); }
        public static TestAggregate FromEvents(IEnumerable<Type> events) { return new TestAggregate(events.ToEvents()); }
        public static TestAggregate FromEvents<T1, T2>() { return FromEvents(Seq.OfTypes<T1, T2>()); }
        public static TestAggregate FromEvents<T1, T2, T3>() { return FromEvents(Seq.OfTypes<T1, T2, T3>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>()); }
        public static TestAggregate FromEvents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>() { return FromEvents(Seq.OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>()); }



        private readonly List<IRootEvent> _history = new List<IRootEvent>();                                                                                                                                                   
        public IReadOnlyList<IRootEvent> History => _history;
    }

    public static class EventSequenceGenerator
    {
        public static IRootEvent[] ToEvents(this IEnumerable<Type> types)
        {
            return types.Select(type => (IRootEvent)Activator.CreateInstance(type)).ToArray();
        }
    }
}

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{

    public class ReplaceE1WithE2 : EventMigration
    {
        public override void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier)
        {
            if(@event is E1)
            {
                modifier.Replace(Seq.Create(new E2()));
            }
        }
    }

    [TestFixture]
    public class EventStreamMutatorTests
    {
        [Test]
        public void TestName()
        {
            var history = TestAggregate.FromEvents<Ec1, E1, E3>().History;

            var result = new EventStreamMutator(new ReplaceE1WithE2()).Mutate(history.ElementAt(1));

            history.ForEach(Console.WriteLine);
        }
    }
}
