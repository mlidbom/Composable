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
                                      1.Through(10)
                                       .SelectMany(
                                           index => 1.Through(900)
                                                     .Select(_ => typeof(E1))
                                                     .Append(typeof(E9))));

            var aggregate = TestAggregate.FromEvents(Guid.NewGuid(), historyTypes);
            _history = aggregate.History.Cast<AggregateRootEvent>().ToList();
        }

        [Test]
        public void With_four_migrations_mutation_takes_less_than_200_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E6>.Insert<E2>(),
                Before<E7>.Insert<E3>(),
                Before<E8>.Insert<E4>(),
                Before<E9>.Insert<E5>()
                ).ToArray();

            TimeAsserter.Execute(
                maxAverage: 200.Milliseconds(),
                iterations: 10,
                description: "load aggregate in isolated scope",
                timeFormat: "ss\\.fff",
                action: () => { SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(eventMigrations, _history); });

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
