using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Logging;
using Composable.Persistence.EventSourcing;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Collections.Collections;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
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
            using(var container = CreateContainerForEventStoreType(() => migrations.ToArray(), EventStoreType))
            {
                var timeSource = container.Resolve<DummyTimeSource>();
                timeSource.UtcNow = DateTime.Parse("2001-01-01 01:01:01.01");
                int scenarioIndex = 1;
                foreach(var migrationScenario in scenarios)
                {
                    timeSource.UtcNow += 1.Hours(); //No time collision between scenarios please.
                    migrations = migrationScenario.Migrations.ToList();
                    RunScenarioWithEventStoreType(migrationScenario, EventStoreType, container, migrations, scenarioIndex++);
                }
            }
        }

        static void RunScenarioWithEventStoreType
            (MigrationScenario scenario, Type eventStoreType, IServiceLocator container, IList<IEventMigration> migrations, int indexOfScenarioInBatch)
        {
            var startingMigrations = migrations.ToList();
            migrations.Clear();

            var timeSource = container.Resolve<DummyTimeSource>();

            IReadOnlyList<IAggregateRootEvent> eventsInStoreAtStart;
            using(container.BeginScope()) //Why is this needed? It fails without it but I do not understand why...
            {
                var eventStore = container.Resolve<IEventStore>();
                eventsInStoreAtStart = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();
            }

            SafeConsole.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

            var original = TestAggregate.FromEvents(DummyTimeSource.Now, scenario.AggregateId, scenario.OriginalHistory).History.ToList();
            SafeConsole.WriteLine("Original History: ");
            original.ForEach(e => SafeConsole.WriteLine($"      {e}"));
            SafeConsole.WriteLine();

            var initialAggregate = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);
            var expected = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.ExpectedHistory).History.ToList();
            var expectedCompleteEventstoreStream = eventsInStoreAtStart.Concat(expected).ToList();

            SafeConsole.WriteLine("Expected History: ");
            expected.ForEach(e => SafeConsole.WriteLine($"      {e}"));
            SafeConsole.WriteLine();

            timeSource.UtcNow += 1.Hours();//Bump clock to ensure that times will be be wrong unless the time from the original events are used..

            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(initialAggregate));
            migrations.AddRange(startingMigrations);
            var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;

            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached aggregate");

            var migratedCachedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
            AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached aggregate");


            SafeConsole.WriteLine("  Streaming all events in store");
            var streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());

            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");

            SafeConsole.WriteLine("  Persisting migrations");
            using(container.BeginScope())
            {
                container.Resolve<IEventStore>().PersistMigrations();
            }

            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

            SafeConsole.WriteLine("Streaming all events in store");
            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
            AssertStreamsAreIdentical( expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");


            SafeConsole.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
            migrations.Clear();

            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
            AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");

            SafeConsole.WriteLine("Streaming all events in store");
            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");

            if(eventStoreType == typeof(SqlServerEventStore))
            {
                SafeConsole.WriteLine("Clearing sql server eventstore cache");
                container.ExecuteUnitOfWorkInIsolatedScope(() => ((SqlServerEventStore)container.Resolve<IEventStore>()).ClearCache());
                migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                SafeConsole.WriteLine("Streaming all events in store");
                streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
                AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");
            }

        }

        protected static IServiceLocator CreateContainerForEventStoreType(Func<IReadOnlyList<IEventMigration>> migrationsfactory, Type eventStoreType, string eventStoreConnectionString = null)
        {
            var container = DependencyInjectionContainer.Create();

            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            container.Register(
                CComponent.For<IEnumerable<IEventMigration>>()
                         .UsingFactoryMethod(_ => migrationsfactory())
                         .LifestyleScoped(),
                CComponent.For<IEventStoreSession, IUnitOfWorkParticipant>()
                         .ImplementedBy<EventStoreSession>()
                         .LifestyleScoped()
                );


            if (eventStoreType == typeof(SqlServerEventStore))
            {
                var cache = new SqlServerEventStoreEventsCache();
                if (eventStoreConnectionString == null)
                {
                    var masterConnectionSTring = new ConnectionStringConfigurationParameterProvider().GetConnectionString("MasterDB");
                    var dbManager = container.RegisterSqlServerDatabasePool(masterConnectionSTring.ConnectionString);

                    eventStoreConnectionString = dbManager.ConnectionStringFor($"{nameof(EventStreamMutatorTestsBase)}_EventStore");
                }

                container.Register(
                                   CComponent.For<IEventStore>()
                                             .UsingFactoryMethod(
                                                                 locator => new SqlServerEventStore(connectionString: eventStoreConnectionString,
                                                                                                    usageGuard: locator.Resolve<ISingleContextUseGuard>(),
                                                                                                    cache: cache,
                                                                                                    nameMapper: null,
                                                                                                    migrations: locator.Resolve<IEnumerable<IEventMigration>>()))
                                             .LifestyleScoped());

            }
            else if(eventStoreType == typeof(InMemoryEventStore))
            {
                container.Register(
                    CComponent.For<IEventStore>()
                             .UsingFactoryMethod(
                                 kernel =>
                                 {
                                     var store = kernel.Resolve<InMemoryEventStore>();
                                     store.TestingOnlyReplaceMigrations(migrationsfactory());
                                     return store;
                                 })
                             .LifestyleScoped(),
                    CComponent.For<InMemoryEventStore>()
                        .ImplementedBy<InMemoryEventStore>()
                        .LifestyleSingleton());
            }
            else
            {
                throw new Exception($"Unsupported type of event store {eventStoreType}");
            }

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            return container.CreateServiceLocator();
        }

        protected static void AssertStreamsAreIdentical(IEnumerable<IAggregateRootEvent> expected, IEnumerable<IAggregateRootEvent> migratedHistory, string descriptionOfHistory)
        {

            try
            {
                expected.ForEach(
                   (@event, index) =>
                   {
                       if (@event.GetType() != migratedHistory.ElementAt(index).GetType())
                       {
                           Assert.Fail(
                               $"Expected event at postion {index} to be of type {@event.GetType()} but it was of type: {migratedHistory.ElementAt(index).GetType()}");
                       }
                   });

                migratedHistory.Cast<AggregateRootEvent>().ShouldAllBeEquivalentTo(
                    expected,
                    config => config.RespectingRuntimeTypes()
                                    .WithStrictOrdering()
                                    .Excluding(@event => @event.EventId)
                                    //.Excluding(@event => @event.UtcTimeStamp)
                                    .Excluding(@event => @event.InsertionOrder)
                                    .Excluding(@event => @event.InsertAfter)
                                    .Excluding(@event => @event.InsertBefore)
                                    .Excluding(@event => @event.Replaces)
                                    .Excluding(@event => @event.InsertedVersion)
                                    .Excluding(@event => @event.ManualVersion)
                                    .Excluding(@event => @event.EffectiveVersion)
                                    .Excluding(subjectInfo => subjectInfo.SelectedMemberPath.EndsWith(".TimeStamp")));
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
