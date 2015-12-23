using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.GenericAbstractions.Time;
using Composable.System.Linq;
using Composable.Windsor;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class InsertingNewEventsTests: SqlServerEventStoreEventStreamMutatorTests
    {
        [Test]
        public void Test01()
        {
      
            var originalHistory = Seq.OfTypes<Ec1, E1, Ef, Ef>();
            var migrations = new EventMigration<IRootEvent>[] { Before<E1>.Insert<E3, E4>(),Before<E4>.Insert<E5>()};
            var aggregateId = Guid.NewGuid();

            var original = TestAggregate.FromEvents(DummyTimeSource.Now, aggregateId, originalHistory).History.ToList();
            Console.WriteLine($"Original History: ");
            original.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine();

            var container = CreateContainerForEventStoreType(migrations, EventStoreType);
            var timeSource = container.Resolve<DummyTimeSource>();
            timeSource.UtcNow = DateTime.Parse("2001-01-01 01:01:01.01");



//            Console.WriteLine($"###############$$$$$$$Running scenario with {EventStoreType}");
//
//            var initialAggregate = TestAggregate.FromEvents(timeSource, aggregateId, originalHistory);
//
//            timeSource.UtcNow += 1.Hours();//Bump clock to ensure that times will be be wrong unless the time from the original events are used..
//
//            Console.WriteLine("Doing pure in memory ");
//            IReadOnlyList<IAggregateRootEvent> otherHistory = SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
//                migrations,
//                initialAggregate2.History.Cast<AggregateRootEvent>().ToList());
//
//            AssertStreamsAreIdentical(expected, otherHistory, $"Direct call to SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory");
//
//            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(initialAggregate));
//            var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
//
//            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");
//
//
//            Console.WriteLine("  Streaming all events in store");
//            var streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
//            AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");
//
//
//            Console.WriteLine("  Persisting migrations");
//            using (container.BeginScope())
//            {
//                container.Resolve<IEventStore>().PersistMigrations();
//            }
//
//            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
//            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");
//
//            Console.WriteLine("Streaming all events in store");
//            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
//            AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");
//
//
//            Console.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
//            migrations = Seq.Empty<IEventMigration>().ToArray();//Disable all migrations so none are used when reading back the history...
//            if (eventStoreType == typeof(InMemoryEventStore))
//            {
//                ((InMemoryEventStore)container.Resolve<IEventStore>()).TestingOnlyReplaceMigrations(migrations);
//            }
//
//            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
//            AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");
//
//            Console.WriteLine("Streaming all events in store");
//            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
//            AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");
//
//            if (eventStoreType == typeof(SqlServerEventStore))
//            {
//                Console.WriteLine("Clearing sql server eventstore cache");
//                container.ExecuteUnitOfWorkInIsolatedScope(() => ((SqlServerEventStore)container.Resolve<IEventStore>()).ClearCache());
//                migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
//                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");
//
//                Console.WriteLine("Streaming all events in store");
//                streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
//                AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");
//            }

        }


    }
}
