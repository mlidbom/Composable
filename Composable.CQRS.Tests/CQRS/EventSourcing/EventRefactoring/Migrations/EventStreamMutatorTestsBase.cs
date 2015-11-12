using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class EventStreamMutatorTestsBase {
        protected void RunMigrationTest
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
    }
}