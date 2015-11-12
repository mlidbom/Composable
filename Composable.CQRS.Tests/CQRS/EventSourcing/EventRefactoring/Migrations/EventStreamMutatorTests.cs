using System;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;


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

    public class ReplaceE1WithE2AndE3 : EventMigration
    {
        public override void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier)
        {
            if (@event is E1)
            {
                modifier.Replace(Seq.Create<IAggregateRootEvent>(new E2(), new E3()));
            }
        }
    }

    public class ReplaceE4WithE5 : EventMigration
    {
        public override void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier)
        {
            if (@event is E4)
            {
                modifier.Replace(Seq.Create(new E5()));
            }
        }
    }

    [TestFixture]
    public class EventStreamMutatorTests
    {
        [Test]
        public void With_single_migration_and_one_event()
        {
            var aggregate = TestAggregate.FromEvents<Ec1, E1>();

            new EventStreamMutator(aggregate.Id, new ReplaceE1WithE2())
                .Mutate(aggregate.History.ElementAt(1))
                .First()
                .Should().BeOfType<E2>();        
        }


        [Test]
        public void With_single_replacement_with_two_resultant_events()
        {
            var aggregate = TestAggregate.FromEvents<Ec1, E1>();

            var mutated = new EventStreamMutator(aggregate.Id, new ReplaceE1WithE2AndE3())
                .Mutate(aggregate.History.ElementAt(1));

            mutated.First().Should().BeOfType<E2>();
            mutated.Second().Should().BeOfType<E3>();
        }

        [Test]
        public void With_two_migrations_and_one_event()
        {
            var aggregate = TestAggregate.FromEvents<Ec1, E1>();

            new EventStreamMutator(aggregate.Id, new ReplaceE1WithE2(), new ReplaceE4WithE5())
                .Mutate(aggregate.History.ElementAt(1))
                .First()
                .Should().BeOfType<E2>();
        }

        [Test]
        public void With_first_replacing_one_event_with_two_and_then_an_unrelated_migration()
        {
            var aggregate = TestAggregate.FromEvents<Ec1, E1>();

            var mutated = new EventStreamMutator(aggregate.Id, new ReplaceE1WithE2AndE3(), new ReplaceE4WithE5())
                .Mutate(aggregate.History.ElementAt(1));

            mutated.First().Should().BeOfType<E2>();
            mutated.Second().Should().BeOfType<E3>();
        }


        [Test]
        public void With_first_replacing_one_event_with_two_and_then_an_unrelated_migration_aoseunth()
        {
            var aggregate = TestAggregate.FromEvents<Ec1, E1>();
            var history = aggregate.History;
            var mutated = new EventStreamMutator(aggregate.Id, new ReplaceE1WithE2AndE3(), new ReplaceE4WithE5())
                .MutateCompleteAggregateHistory(history);

            var expected = TestAggregate.FromEvents<Ec1, E2, E3>(history.First().AggregateRootId).History;

            mutated.ShouldAllBeEquivalentTo(
                expected, config => config.RespectingRuntimeTypes()
                .WithStrictOrdering()
                .Excluding(@event => @event.EventId)               
                .Excluding(@event => @event.TimeStamp));

        }
    }
}
