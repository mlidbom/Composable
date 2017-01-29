using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.Testing;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.System.Collections.Collections;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using Composable.Windsor;
using Composable.Windsor.Testing;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    //Todo: Refactor this test. It is too monolithic and hard to read and extend.
    public abstract class EventStreamMutatorTestsBase
    {
        protected readonly Type EventStoreType;
        protected EventStreamMutatorTestsBase(Type eventStoreType) {
            EventStoreType = eventStoreType;
        }

        protected void RunMigrationTest(params MigrationScenario[] scenarios)
        {
            Console.WriteLine($"###############$$$$$$$Running {scenarios.Length} scenario(s) with EventStoreType: {EventStoreType}");


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

        private static void RunScenarioWithEventStoreType
            (MigrationScenario scenario, Type eventStoreType, WindsorContainer container, IList<IEventMigration> migrations, int indexOfScenarioInBatch)
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

            Console.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

            var original = TestAggregate.FromEvents(DummyTimeSource.Now, scenario.AggregateId, scenario.OriginalHistory).History.ToList();
            Console.WriteLine($"Original History: ");
            original.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine();            

            var initialAggregate = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);
            var expected = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.ExpectedHistory).History.ToList();
            var expectedCompleteEventstoreStream = eventsInStoreAtStart.Concat(expected).ToList();

            Console.WriteLine($"Expected History: ");
            expected.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine();

            var initialAggregate2 = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);

            timeSource.UtcNow += 1.Hours();//Bump clock to ensure that times will be be wrong unless the time from the original events are used..

            Console.WriteLine("Doing pure in memory ");            
            IReadOnlyList<IAggregateRootEvent> otherHistory = SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                scenario.Migrations,
                initialAggregate2.History.Cast<AggregateRootEvent>().ToList());

            AssertStreamsAreIdentical(expected, otherHistory, $"Direct call to SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory");

            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(initialAggregate));
            migrations.AddRange(startingMigrations);
            var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;            

            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached aggregate");

            var migratedCachedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
            AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached aggregate");


            Console.WriteLine("  Streaming all events in store");
            var streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
            
            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");
            
            Console.WriteLine("  Persisting migrations");
            using(container.BeginScope())
            {
                container.Resolve<IEventStore>().PersistMigrations();
            }

            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;            
            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

            Console.WriteLine("Streaming all events in store");
            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
            AssertStreamsAreIdentical( expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");


            Console.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
            migrations.Clear();

            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
            AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");

            Console.WriteLine("Streaming all events in store");
            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
            AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");

            if(eventStoreType == typeof(SqlServerEventStore))
            {
                Console.WriteLine("Clearing sql server eventstore cache");
                container.ExecuteUnitOfWorkInIsolatedScope(() => ((SqlServerEventStore)container.Resolve<IEventStore>()).ClearCache());
                migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                Console.WriteLine("Streaming all events in store");
                streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList());
                AssertStreamsAreIdentical(expectedCompleteEventstoreStream, streamedEvents, "Streaming all events in store");
            }

        }

        protected static WindsorContainer CreateContainerForEventStoreType(Func<IReadOnlyList<IEventMigration>> migrationsfactory, Type eventStoreType, string eventStoreConnectionString = null)
        {
            var container = new WindsorContainer();

            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            container.Register(
                Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                    .Instance(DummyTimeSource.Now)
                    .LifestyleSingleton(),
                Component.For<IServiceBus>()
                         .ImplementedBy<SynchronousBus>()
                         .LifestylePerWebRequest(),
                Component.For<IEnumerable<IEventMigration>>()
                         .UsingFactoryMethod(migrationsfactory)
                         .LifestylePerWebRequest(),
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>()
                         .ImplementedBy<EventStoreSession>()
                         .LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(container)
                );


            if (eventStoreType == typeof(SqlServerEventStore))
            {
                if(eventStoreConnectionString == null)
                {
                    var masterConnectionSTring = new ConnectionStringConfigurationParameterProvider().GetConnectionString("MasterDB");
                    var dbManager = new SqlServerDatabasePool(masterConnectionSTring.ConnectionString, container);

                    eventStoreConnectionString = dbManager.ConnectionStringFor($"{nameof(EventStreamMutatorTestsBase)}_EventStore");
                }

                container.Register(                    
                    Component.For<IEventStore>()
                             .ImplementedBy<SqlServerEventStore>()
                             .DependsOn(Dependency.OnValue<string>(eventStoreConnectionString))
                             .LifestyleScoped());

            }
            else if(eventStoreType == typeof(InMemoryEventStore))
            {
                container.Register(
                    Component.For<IEventStore>()
                             .UsingFactoryMethod(
                                 kernel =>
                                 {
                                     var store = kernel.Resolve<InMemoryEventStore>();
                                     store.TestingOnlyReplaceMigrations(migrationsfactory());
                                     return store;
                                 })
                             .LifestyleScoped(),
                    Component.For<InMemoryEventStore>()
                        .ImplementedBy<InMemoryEventStore>()
                        .LifestyleSingleton());
            }
            else
            {
                throw new Exception($"Unsupported type of event store {eventStoreType}");
            }            

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            return container;
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
                Console.WriteLine($"   Failed comparing with {descriptionOfHistory}");
                Console.WriteLine($"   Expected: ");
                expected.ForEach(e => Console.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
                Console.WriteLine($"\n   Actual: ");
                migratedHistory.ForEach(e => Console.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
                Console.WriteLine("\n");               

                throw;
            }
        }
    }
}
