using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using Composable.Windsor;
using Composable.Windsor.Testing;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NCrunch.Framework;
using NUnit.Framework;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public abstract class EventStreamMutatorTestsBase
    {
        protected readonly Type EventStoreType;
        protected EventStreamMutatorTestsBase(Type eventStoreType) {
            EventStoreType = eventStoreType;
        }

        protected static string ConnectionString => ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;

        protected void RunMigrationTest
            (
            IEnumerable<Type> originalHistory,
            IEnumerable<Type> expectedHistory,
            params IEventMigration[] manualMigrations)
        {
            var migrationInstances = manualMigrations;
            var aggregateId = Guid.NewGuid();

            var original = TestAggregate.FromEvents(aggregateId, originalHistory).History.ToList();
            Console.WriteLine($"Original History: ");
            original.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine();

            RunScenarioWithEventStoreType(originalHistory, expectedHistory, aggregateId, migrationInstances, EventStoreType);
        }

        private static void RunScenarioWithEventStoreType
            (IEnumerable<Type> originalHistory,
             IEnumerable<Type> expectedHistory,
             Guid aggregateId,
             IEventMigration[] migrations,
             Type eventStoreType)
        {
            var container = CreateContainerForEventStoreType(migrations, eventStoreType);

            Console.WriteLine($"###############$$$$$$$Running scenario with {eventStoreType}");

            var initialAggregate = TestAggregate.FromEvents(aggregateId, originalHistory);
            var expected = TestAggregate.FromEvents(aggregateId, expectedHistory).History.ToList();

            Console.WriteLine("Doing pure in memory ");
            var initialAggregate2 = TestAggregate.FromEvents(aggregateId, originalHistory);
            IReadOnlyList<IAggregateRootEvent> otherHistory = SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                migrations,
                initialAggregate2.History.Cast<AggregateRootEvent>().ToList());

            AssertStreamsAreIdentical(expected, otherHistory, $"Direct call to SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory");

            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(initialAggregate));
            var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;            

            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");


            Console.WriteLine("  Streaming all events in store");
            var streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().StreamEvents().ToList());
            AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");


            Console.WriteLine("  Persisting migrations");
            using(container.BeginScope())
            {
                container.Resolve<IEventStore>().PersistMigrations();
            }

            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;            
            AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

            Console.WriteLine("Streaming all events in store");
            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().StreamEvents().ToList());
            AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");


            Console.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
            migrations = Seq.Empty<IEventMigration>().ToArray();//Disable all migrations so none are used when reading back the history...
            if (eventStoreType == typeof(InMemoryEventStore))
            {
                ((InMemoryEventStore)container.Resolve<IEventStore>()).TestingOnlyReplaceMigrations(migrations);
            }

            migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
            AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");

            Console.WriteLine("Streaming all events in store");
            streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().StreamEvents().ToList());
            AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");

            if(eventStoreType == typeof(SqlServerEventStore))
            {
                Console.WriteLine("Clearing sql server eventstore cache");
                container.ExecuteUnitOfWorkInIsolatedScope(() => ((SqlServerEventStore)container.Resolve<IEventStore>()).ClearCache());
                migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;
                AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

                Console.WriteLine("Streaming all events in store");
                streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().StreamEvents().ToList());
                AssertStreamsAreIdentical(expected, streamedEvents, "Streaming all events in store");
            }

        }

        protected static WindsorContainer CreateContainerForEventStoreType(IEventMigration[] migrations, Type eventStoreType)
        {
            var container = new WindsorContainer();

            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            container.Register(
                Component.For<IUtcTimeTimeSource>()
                    .ImplementedBy<DateTimeNowTimeSource>()
                    .LifestyleSingleton(),
                Component.For<IServiceBus>()
                         .ImplementedBy<SynchronousBus>()
                         .LifestylePerWebRequest(),
                Component.For<IEnumerable<IEventMigration>>()
                         .UsingFactoryMethod(() => migrations)
                         .LifestylePerWebRequest(),
                SelectLifeStyle(
                    Component.For<IEventStore>()
                             .ImplementedBy(eventStoreType)
                             .DependsOn(Dependency.OnValue<string>(ConnectionString))),
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>()
                         .ImplementedBy<EventStoreSession>()
                         .LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(container)
                );

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
            return container;
        }
        private static IRegistration SelectLifeStyle(ComponentRegistration<IEventStore> dependsOn)
        {
            if(dependsOn.Implementation == typeof(SqlServerEventStore))
            {
                return dependsOn.LifestylePerWebRequest();
            }

            if (dependsOn.Implementation == typeof(InMemoryEventStore))
            {
                return dependsOn.LifestyleSingleton();
            }

            throw new Exception($"Unsupported type of event store {dependsOn.Implementation}");
        }

        private static void AssertStreamsAreIdentical(List<IAggregateRootEvent> expected, IReadOnlyList<IAggregateRootEvent> migratedHistory, string descriptionOfHistory)
        {

            try
            {
                expected.ForEach(
                   (@event, index) =>
                   {
                       if (@event.GetType() != migratedHistory[index].GetType())
                       {
                           Assert.Fail(
                               $"Expected event at postion {index} to be of type {@event.GetType()} but it was of type: {migratedHistory[index].GetType()}");
                       }
                   });

                migratedHistory.Cast<AggregateRootEvent>().ShouldAllBeEquivalentTo(
                    expected,
                    config => config.RespectingRuntimeTypes()
                                    .WithStrictOrdering()
                                    .Excluding(@event => @event.EventId)
                                    .Excluding(@event => @event.UtcTimeStamp)
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
                expected.ForEach(e => Console.WriteLine($"      {e}"));
                Console.WriteLine($"\n   Actual: ");
                migratedHistory.ForEach(e => Console.WriteLine($"      {e}"));
                Console.WriteLine("\n");               

                throw;
            }
        }
    }
}
