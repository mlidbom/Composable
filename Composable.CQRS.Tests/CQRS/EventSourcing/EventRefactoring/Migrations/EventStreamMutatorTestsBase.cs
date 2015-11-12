using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;
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

            var mutatedHistory = new SingleAggregateEventStreamMutator(aggregate.Id, migrationInstances)
                .MutateCompleteAggregateHistory(aggregate.History).ToList();

            var expected = TestAggregate.FromEvents(aggregateId, expectedHistory).History.ToList();


            Console.WriteLine($"Expected: ");
            expected.ForEach(Console.WriteLine);
            Console.WriteLine($"\nActual: ");
            mutatedHistory.ForEach(Console.WriteLine);


            expected.ForEach(
                (@event, index) =>
                {
                    if(@event.GetType() != mutatedHistory[index].GetType())
                    {
                        Assert.Fail($"Expected event at postion {index} to be of type {@event.GetType()} but it was of type: {mutatedHistory[index].GetType()}");
                    }  
                });

            mutatedHistory.ShouldAllBeEquivalentTo(
                expected,
                config => config.RespectingRuntimeTypes()
                                .WithStrictOrdering()
                                .Excluding(@event => @event.EventId)
                                .Excluding(@event => @event.TimeStamp));            

        }
    }
}