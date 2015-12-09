using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class EventMigrationsScenario
    {
        private readonly IWindsorContainer _container;
        public EventMigrationsScenario(IWindsorContainer container) { _container = container; }

        public void RunMigrationTest
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
            expected.ForEach(e => Console.WriteLine($"   {e}"));

            Console.WriteLine($"\nActual: ");
            mutatedHistory.ForEach(e => Console.WriteLine($"   {e}"));

            expected.ForEach(
                (@event, index) =>
                {
                    if(@event.GetType() != mutatedHistory[index].GetType())
                    {
                        Assert.Fail(
                            $"Expected event at postion {index} to be of type {@event.GetType()} but it was of type: {mutatedHistory[index].GetType()}");
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
