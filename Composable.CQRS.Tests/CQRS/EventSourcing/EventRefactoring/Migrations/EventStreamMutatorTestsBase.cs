using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

using IEventStore = Composable.DependencyInjection.Persistence.IEventStore<Composable.CQRS.Tests.ITestingEventstoreUpdater, Composable.CQRS.Tests.ITestingEventstoreReader>;

// ReSharper disable AccessToModifiedClosure

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture]
    //Todo: Refactor this test. It is too monolithic and hard to read and extend.
    public abstract class EventStreamMutatorTestsBase
    {
        protected readonly Type EventStoreType;
        protected EventStreamMutatorTestsBase(Type eventStoreType) => EventStoreType = eventStoreType;

        internal void RunMigrationTest(params MigrationScenario[] scenarios)
        {
            SafeConsole.WriteLine($"###############$$$$$$$Running {scenarios.Length} scenario(s) with EventStoreType: {EventStoreType}");

            IList<IEventMigration> migrations = new List<IEventMigration>();
            using(var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations.ToArray(), EventStoreType))
            {
                var timeSource = serviceLocator.Resolve<DummyTimeSource>();
                timeSource.UtcNow = DateTime.Parse("2001-01-01 01:01:01.01");
                int scenarioIndex = 1;
                foreach(var migrationScenario in scenarios)
                {
                    timeSource.UtcNow += 1.Hours(); //No time collision between scenarios please.
                    migrations = migrationScenario.Migrations.ToList();
                    RunScenarioWithEventStoreType(migrationScenario, EventStoreType, serviceLocator, migrations, scenarioIndex++);
                }
            }
        }

        static void RunScenarioWithEventStoreType(MigrationScenario scenario, Type eventStoreType, IServiceLocator serviceLocator, IList<IEventMigration> migrations, int indexOfScenarioInBatch)
        {
            var startingMigrations = migrations.ToList();
            migrations.Clear();

            var timeSource = serviceLocator.Resolve<DummyTimeSource>();

            IReadOnlyList<IAggregateRootEvent> eventsInStoreAtStart;
            using(serviceLocator.BeginScope()) //Why is this needed? It fails without it but I do not understand why...
            {
                var eventStore = serviceLocator.Resolve<IEventStore>();
                eventsInStoreAtStart = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();
            }

            SafeConsole.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

            var original = TestAggregate.FromEvents(DummyTimeSource.Now, scenario.AggregateId, scenario.OriginalHistory)
                                        .History.ToList();
            SafeConsole.WriteLine("Original History: ");
            original.ForEach(e => SafeConsole.WriteLine($"      {e}"));
            SafeConsole.WriteLine();

            var initialAggregate = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);
            var expected = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.ExpectedHistory)
                                        .History.ToList();
            var expectedCompleteEventstoreStream = eventsInStoreAtStart.Concat(expected)
                                                                       .ToList();

            SafeConsole.WriteLine("Expected History: ");
            expected.ForEach(e => SafeConsole.WriteLine($"      {e}"));
            SafeConsole.WriteLine();

            timeSource.UtcNow += 1.Hours(); //Bump clock to ensure that times will be be wrong unless the time from the original events are used..

            serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<ITestingEventstoreUpdater>()
                                                                                .Save(initialAggregate));
            migrations.AddRange(startingMigrations);
            var migratedHistory = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<ITestingEventstoreUpdater>()
                                                                                                      .Get<TestAggregate>(initialAggregate.Id))
                                                .History;

            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached aggregate");

            var migratedCachedHistory = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<ITestingEventstoreUpdater>()
                                                                                                            .Get<TestAggregate>(initialAggregate.Id))
                                                      .History;
            AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached aggregate");

            SafeConsole.WriteLine("  Streaming all events in store");
            var streamedEvents = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                                     .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                     .ToList());

            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");

            SafeConsole.WriteLine("  Persisting migrations");
            using(serviceLocator.BeginScope())
            {
                serviceLocator.Resolve<IEventStore>()
                              .PersistMigrations();
            }

            migratedHistory = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<ITestingEventstoreUpdater>()
                                                                                                  .Get<TestAggregate>(initialAggregate.Id))
                                            .History;
            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

            SafeConsole.WriteLine("Streaming all events in store");
            streamedEvents = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                                 .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                 .ToList());
            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");

            SafeConsole.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
            migrations.Clear();

            migratedHistory = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<ITestingEventstoreUpdater>()
                                                                                                  .Get<TestAggregate>(initialAggregate.Id))
                                            .History;
            AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");

            SafeConsole.WriteLine("Streaming all events in store");
            streamedEvents = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                                 .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                 .ToList());
            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");

            if(eventStoreType == typeof(EventStore))
            {
                SafeConsole.WriteLine("Clearing sql server eventstore cache");
                serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => ((EventStore)serviceLocator.Resolve<IEventStore>()).ClearCache());
                migratedHistory = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<ITestingEventstoreUpdater>()
                                                                                                      .Get<TestAggregate>(initialAggregate.Id))
                                                .History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                SafeConsole.WriteLine("Streaming all events in store");
                streamedEvents = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                                     .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                     .ToList());
                AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");
            }
        }

        protected static IServiceLocator CreateServiceLocatorForEventStoreType(Func<IReadOnlyList<IEventMigration>> migrationsfactory, Type eventStoreType)
        {
            var serviceLocator = DependencyInjectionContainer.CreateServiceLocatorForTesting(
                container =>
                {
                    var eventStoreConnectionString = container.CreateServiceLocator().EventStoreConnectionString();
                    if(eventStoreType == typeof(EventStore))
                    {
                       container.RegisterSqlServerEventStoreForFlexibleTesting<ITestingEventstoreUpdater, ITestingEventstoreReader>(TestingMode.RealComponents, eventStoreConnectionString, migrationsfactory);
                    } else if(eventStoreType == typeof(InMemoryEventStore))
                    {
                        container.RegisterSqlServerEventStoreForFlexibleTesting<ITestingEventstoreUpdater, ITestingEventstoreReader>(TestingMode.InMemory, eventStoreConnectionString, migrationsfactory);
                    } else
                    {
                        throw new Exception($"Unsupported type of event store {eventStoreType}");
                    }
                });

            return serviceLocator;
        }

        protected static void AssertStreamsAreIdentical(IEnumerable<IAggregateRootEvent> expected, IEnumerable<IAggregateRootEvent> migratedHistory, string descriptionOfHistory)
        {
            try
            {
                expected.ForEach(
                    (@event, index) =>
                    {
                        if(@event.GetType() != migratedHistory.ElementAt(index)
                                                              .GetType())
                        {
                            Assert.Fail(
                                $"Expected event at postion {index} to be of type {@event.GetType()} but it was of type: {migratedHistory.ElementAt(index) .GetType()}");
                        }
                    });

                migratedHistory.Cast<AggregateRootEvent>()
                               .ShouldAllBeEquivalentTo(
                                   expected,
                                   config => config.RespectingRuntimeTypes()
                                                   .WithStrictOrdering()
                                                   .Excluding(@event => @event.EventId)
                                                   .Excluding(@event => @event.InsertionOrder)
                                                   .Excluding(@event => @event.InsertAfter)
                                                   .Excluding(@event => @event.InsertBefore)
                                                   .Excluding(@event => @event.Replaces)
                                                   .Excluding(@event => @event.InsertedVersion)
                                                   .Excluding(@event => @event.ManualVersion)
                                                   .Excluding(@event => @event.EffectiveVersion)
                                                   );
            }
            catch(Exception)
            {
                SafeConsole.WriteLine($"   Failed comparing with {descriptionOfHistory}");
                SafeConsole.WriteLine("   Expected: ");
                expected.ForEach(e => SafeConsole.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
                SafeConsole.WriteLine("\n   Actual: ");
                migratedHistory.ForEach(e => SafeConsole.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
                SafeConsole.WriteLine("\n");

                throw;
            }
        }
    }
}
