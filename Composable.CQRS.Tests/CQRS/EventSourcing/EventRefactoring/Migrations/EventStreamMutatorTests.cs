using System;
using System.Collections.Generic;
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

    public class EventTypeReplacer<TEvent> : EventMigration
    {
        private readonly IEnumerable<Type> _replaceWith;

        public EventTypeReplacer(IEnumerable<Type> replaceWith) { _replaceWith = replaceWith; }

        public override void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier)
        {
            if(@event.GetType() == typeof(TEvent))
            {
                modifier.Replace(_replaceWith.Select(Activator.CreateInstance).Cast<IAggregateRootEvent>().ToList());
            }
        }
    }    

    [TestFixture]
    public class EventStreamMutatorTests
    {

        private void RunMigrationTest
            (
            IEnumerable<Type> originalHistory,
            IEnumerable<Type> expectedHistory,
            params IEventMigration[] manualMigrations)
        {
            var migrationInstances = manualMigrations;
            var aggregateId = Guid.NewGuid();
            var aggregate = TestAggregate.FromEvents(aggregateId, originalHistory);

            var mutatedHistory = new EventStreamMutator(aggregate.Id, migrationInstances)
                .MutateCompleteAggregateHistory(aggregate.History).ToList();

            var expected = TestAggregate.FromEvents(aggregateId, expectedHistory).History.ToList();

            mutatedHistory.ShouldAllBeEquivalentTo(
                expected,
                config => config.RespectingRuntimeTypes()
                                .WithStrictOrdering()
                                .Excluding(@event => @event.EventId)
                                .Excluding(@event => @event.TimeStamp));

            expected.ForEach((@event, index) => mutatedHistory[index].GetType().Should().Be(@event.GetType(), $"Event at index: {index}"));

        }

        [Test]
        public void Replacing_one_event_with_one_event()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2>(),
                new EventTypeReplacer<E1>(replaceWith: Seq.OfTypes<E2>()));
        }


        [Test]
        public void Replacing_E1_with_E2_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2, E3>(),
                new EventTypeReplacer<E1>(replaceWith: Seq.OfTypes<E2, E3>()));
        }

        [Test]
        public void Replacing_E1_with_E2_then_irrelevant_migration()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2>(),
                new EventTypeReplacer<E1>(replaceWith: Seq.OfTypes<E2>()),
                new EventTypeReplacer<E4>(replaceWith: Seq.OfTypes<E5>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2, E3>(),
                new EventTypeReplacer<E1>(replaceWith: Seq.OfTypes<E2, E3>()),
                new EventTypeReplacer<E4>(replaceWith: Seq.OfTypes<E5>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_E2_with_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E4, E3>(),
                new EventTypeReplacer<E1>(replaceWith: Seq.OfTypes<E2, E3>()),
                new EventTypeReplacer<E2>(replaceWith: Seq.OfTypes<E4>()));
        }
    }
}