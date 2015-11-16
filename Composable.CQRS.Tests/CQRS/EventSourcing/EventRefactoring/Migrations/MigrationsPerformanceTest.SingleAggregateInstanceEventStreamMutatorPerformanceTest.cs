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
    public class SingleAggregateInstanceEventStreamMutatorPerformanceTest
    {
        private List<AggregateRootEvent> _history;
        [SetUp]
        public void Given_a_10000_events_large_aggregate()
        {
            var historyTypes = Seq.OfTypes<Ec1>()
                                  .Concat(
                                      1.Through(100)
                                       .SelectMany(
                                           index => 1.Through(996)
                                                     .Select(_ => typeof(E1))
                                                     .Concat(Seq.OfTypes<E2,E4,E6,E8>())));

            var aggregate = TestAggregate.FromEvents(Guid.NewGuid(), historyTypes);
            _history = aggregate.History.Cast<AggregateRootEvent>().ToList();
        }

        [Test]
        public void With_four_migrations_mutation_that_all_actually_changes_things_migration_takes_less_than_200_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E2>.Insert<E3>(),
                Before<E4>.Insert<E5>(),
                Before<E6>.Insert<E7>(),
                Before<E8>.Insert<E9>()
                ).ToArray();

            TimeAsserter.Execute(
                maxAverage: 1000.Milliseconds(),
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });

        }

        [Test]
        public void With_four_migrations_that_change_nothing_mutation_takes_less_than_200_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E3>.Insert<E1>(),
                Before<E5>.Insert<E1>(),
                Before<E7>.Insert<E1>(),
                Before<E9>.Insert<E1>()
                ).ToArray();

            TimeAsserter.Execute(
                maxAverage: 1000.Milliseconds(),
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });

        }

        [Test]
        public void Calling_before_after_or_replace_1000000_times_takes_less_than_30_milliseconds()
        {
            var before = Before<E3>.Insert<E2>().CreateMigrator();
            var replace = Replace<E3>.With<E2>().CreateMigrator();
            var after = After<E3>.Insert<E2>().CreateMigrator();
            var @event = new E2();
            var eventModifier = new EventModifier(@event, _ => { });

            var numberOfEventsToInspect = 1000000;
            var maxtime = 800.Seconds();

            TimeAsserter.Execute(maxTotal: maxtime, description: $"{nameof(before)}", iterations: numberOfEventsToInspect, action: () => before.MigrateEvent(@event, @eventModifier));
            TimeAsserter.Execute(maxTotal: maxtime, description: $"{nameof(replace)}", iterations: numberOfEventsToInspect, action: () => replace.MigrateEvent(@event, @eventModifier));
            TimeAsserter.Execute(maxTotal: maxtime, description: $"{nameof(after)}", iterations: numberOfEventsToInspect, action: () => after.MigrateEvent(@event, @eventModifier));
        }

        [Test]
        public void When_there_are_no_migrations_mutation_takes_less_than_a_millisecond()
        {
            var eventMigrations = Seq.Create<IEventMigration>().ToArray();

            TimeAsserter.Execute(
                maxAverage: 1.Milliseconds(),
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });
        }
    }
}
