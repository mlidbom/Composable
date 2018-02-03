using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.DependencyInjection.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Logging;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Refactoring.Naming;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using ITestingEventStore = Composable.DependencyInjection.Persistence.IEventStore<Composable.Tests.ITestingEventStoreUpdater, Composable.Tests.ITestingEventStoreReader>;

// ReSharper disable AccessToModifiedClosure

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TestFixture]
    //refactor: this test. It is too monolithic and hard to read and extend.
    public abstract class EventMigrationTestBase
    {
        protected readonly Type EventStoreType;
        protected EventMigrationTestBase(Type eventStoreType) => EventStoreType = eventStoreType;

        internal void RunMigrationTest(params MigrationScenario[] scenarios)
        {
            SafeConsole.WriteLine($"###############$$$$$$$Running {scenarios.Length} scenario(s) with EventStoreType: {EventStoreType}");

            IList<IEventMigration> migrations = new List<IEventMigration>();
            using(var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations.ToArray(), EventStoreType))
            {
                var timeSource = serviceLocator.Resolve<TestingTimeSource>();
                timeSource.FreezeAtUtcTime(DateTime.Parse("2001-01-01 01:01:01.01"));
                var scenarioIndex = 1;
                foreach(var migrationScenario in scenarios)
                {
                    timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //No time collision between scenarios please.
                    migrations = migrationScenario.Migrations.ToList();
                    RunScenarioWithEventStoreType(migrationScenario, serviceLocator, migrations, scenarioIndex++);
                }
            }
        }

        static void RunScenarioWithEventStoreType(MigrationScenario scenario, IServiceLocator serviceLocator, IList<IEventMigration> migrations, int indexOfScenarioInBatch)
        {
            var startingMigrations = migrations.ToList();
            migrations.Clear();

            var timeSource = serviceLocator.Resolve<TestingTimeSource>();

            IReadOnlyList<IAggregateEvent> eventsInStoreAtStart;
            using(serviceLocator.BeginScope()) //Why is this needed? It fails without it but I do not understand why...
            {
                var eventStore = serviceLocator.Resolve<ITestingEventStore>();
                eventsInStoreAtStart = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();
            }

            SafeConsole.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

            var original = TestAggregate.FromEvents(TestingTimeSource.FrozenUtcNow(), scenario.AggregateId, scenario.OriginalHistory)
                                        .History.ToList();
            SafeConsole.WriteLine("Original History: ");
            original.ForEach(e => SafeConsole.WriteLine($"      {e}"));
            SafeConsole.WriteLine();

            var initialAggregate = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);
            var expected = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.ExpectedHistory)
                                        .History.ToList();
            var expectedCompleteEventStoreStream = eventsInStoreAtStart.Concat(expected)
                                                                       .ToList();

            SafeConsole.WriteLine("Expected History: ");
            expected.ForEach(e => SafeConsole.WriteLine($"      {e}"));
            SafeConsole.WriteLine();

            timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //Bump clock to ensure that times will be be wrong unless the time from the original events are used..

            serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                .Save(initialAggregate));
            migrations.AddRange(startingMigrations);
            ClearCache(serviceLocator);

            var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                      .Get<TestAggregate>(initialAggregate.Id))
                                                .History;

            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached aggregate");

            var migratedCachedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                            .Get<TestAggregate>(initialAggregate.Id))
                                                      .History;
            AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached aggregate");

            SafeConsole.WriteLine("  Streaming all events in store");
            var streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStore>()
                                                                                                     .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                     .ToList());

            AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");


            //Make sure that other processes that might be using the same aggregate also keep working as we persist the migrations.
            using(var clonedServiceLocator = serviceLocator.Clone())
            {
                migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                                  .Get<TestAggregate>(initialAggregate.Id))
                                                      .History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                SafeConsole.WriteLine("  Persisting migrations");
                using(serviceLocator.BeginScope())
                {
                    serviceLocator.Resolve<ITestingEventStore>()
                                  .PersistMigrations();
                }

                migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                      .Get<TestAggregate>(initialAggregate.Id))
                                                .History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                                  .Get<TestAggregate>(initialAggregate.Id))

                                                      .History;
            }
            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

            SafeConsole.WriteLine("Streaming all events in store");
            streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStore>()
                                                                                                 .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                 .ToList());
            AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");

            SafeConsole.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
            migrations.Clear();

            migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                  .Get<TestAggregate>(initialAggregate.Id))
                                            .History;
            AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");

            SafeConsole.WriteLine("Streaming all events in store");
            streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITestingEventStore>()
                                                                                                 .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                 .ToList());
            AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");


            SafeConsole.WriteLine("Cloning service locator / starting new instance of application");
            using(var clonedServiceLocator2 = serviceLocator.Clone())
            {

                migratedHistory = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<ITestingEventStoreUpdater>()
                                                                                                        .Get<TestAggregate>(initialAggregate.Id))
                                                .History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                SafeConsole.WriteLine("Streaming all events in store");
                streamedEvents = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<ITestingEventStore>()
                                                                                                        .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                        .ToList());
                AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");
            }
        }
        protected static void ClearCache(IServiceLocator serviceLocator)
        {
            serviceLocator.ExecuteInIsolatedScope(() =>
            {
                if(serviceLocator.Resolve<ITestingEventStore>() is EventStore)
                {
                    serviceLocator.Resolve<SqlServerEventStoreRegistrationExtensions.EventCache<ITestingEventStoreUpdater>>().Clear();
                }
            });
        }

        protected static IServiceLocator CreateServiceLocatorForEventStoreType(Func<IReadOnlyList<IEventMigration>> migrationsfactory, Type eventStoreType)
        {
            TestingMode mode;
            if (eventStoreType == typeof(EventStore))
            {
                mode = TestingMode.DatabasePool;
            }
            else if (eventStoreType == typeof(InMemoryEventStore))
            {
                mode = TestingMode.InMemory;
            }
            else
            {
                throw new Exception($"Unsupported type of event store {eventStoreType}");
            }

            var serviceLocator = DependencyInjectionContainer.CreateServiceLocatorForTesting(
                container =>
                    container.RegisterSqlServerEventStoreForFlexibleTesting<ITestingEventStoreUpdater, ITestingEventStoreReader>(TestWiringHelper.EventStoreConnectionStringName, migrationsfactory),
                mode);

            serviceLocator.Resolve<ITypeMappingRegistar>()
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.TestAggregate>("dbc5cd48-bc09-4d96-804d-6712493a413d")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E1>("cdb56e08-9ccb-497a-89cd-230913a51877")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E2>("808a5fed-4925-4b2c-8992-fd75521959e6")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E3>("9297ccdd-0a0b-4632-8c86-2634f75822bf")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E4>("aa67591a-2a91-4e74-9cc3-0991f72473bc")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E5>("32979722-64d1-4113-af1d-c5f7c2c6862c")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E6>("08bfb660-adc4-480f-82d5-db64fa9a0ac5")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E7>("d8a2ea4f-7dad-4658-8530-a50f092f0640")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E8>("70424c93-f24c-44c9-a1d6-fb2d6fe83e0a")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.E9>("ec965ddd-5a8a-4fef-890f-4f302069e8ba")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.Ec1>("117fc595-4756-4695-a907-43d0501bf32c")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.Ec2>("3d0e3a47-989d-4096-9389-79d6960ee6d6")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.Ec3>("76b2bbce-b5b4-4293-b707-85cbbaeb7916")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.EcAbstract>("74797038-5f9b-4660-b853-fa81ad67f193")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.Events.Ef>("19f36c9a-6f42-429a-9d43-26532e718ceb")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.IRootEvent>("a846112e-86ce-4dc5-ac7b-97bb44f8e1ce")
                          .Map<Composable.Tests.CQRS.EventRefactoring.Migrations.RootEvent>("a3714dd8-1c20-47be-bb5a-a17ee2c5656f")
                          .Map<Composable.Tests.CQRS.IUserEvent>("059a8d68-9b84-4e6b-85b6-fb3e0f7d9d6f")
                          .Map<Composable.Tests.CQRS.MigratedAfterUserChangedEmailEvent>("ebda8f29-0e76-493f-b4d5-220b9605de13")
                          .Map<Composable.Tests.CQRS.MigratedBeforeUserRegisteredEvent>("3b3477ab-014b-4dbf-921d-8569d7e934e2")
                          .Map<Composable.Tests.CQRS.MigratedReplaceUserChangedPasswordEvent>("fa51dab5-d012-491a-b73e-5b343d9aa2d0")
                          .Map<Composable.Tests.CQRS.UserChangedEmail>("67c06a44-56eb-4b67-b6e5-ef125653ed7c")
                          .Map<Composable.Tests.CQRS.UserChangedPassword>("bbcad7d4-e5f6-45b1-8dd5-99d54b048e3a")
                          .Map<Composable.Tests.CQRS.UserEvent>("507c052d-eeaf-402f-9f2b-91941118caf2")
                          .Map<Composable.Tests.CQRS.UserRegistered>("02feaed0-b540-4402-92b2-30073db53fa1");

            return serviceLocator;
        }

        protected static void AssertStreamsAreIdentical(IEnumerable<IAggregateEvent> expected, IEnumerable<IAggregateEvent> migratedHistory, string descriptionOfHistory)
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

                migratedHistory.Cast<AggregateEvent>()
                               .ShouldAllBeEquivalentTo(
                                   expected,
                                   config => config.RespectingRuntimeTypes()
                                                   .WithStrictOrdering()
                                                   .Excluding(@event => @event.MessageId)
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
