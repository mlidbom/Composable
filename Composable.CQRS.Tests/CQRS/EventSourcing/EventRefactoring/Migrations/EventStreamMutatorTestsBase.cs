using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class EventStreamMutatorTestsBase
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;
        [SetUp]
        public void SetupTask() {
            SqlServerEventStore.ResetDB(ConnectionString);   
        }

        protected void RunMigrationTest
            (
            IEnumerable<Type> originalHistory,
            IEnumerable<Type> expectedHistory,
            params Func<IEventMigration>[] manualMigrations)
        {
            var migrationInstances = manualMigrations;
            var aggregateId = Guid.NewGuid();

            RunScenarioWithEventStoreType(originalHistory, expectedHistory, aggregateId, migrationInstances, typeof(SqlServerEventStore));
            RunScenarioWithEventStoreType(originalHistory, expectedHistory, aggregateId, migrationInstances, typeof(InMemoryEventStore));            
        }

        private static void RunScenarioWithEventStoreType
            (IEnumerable<Type> originalHistory,
             IEnumerable<Type> expectedHistory,
             Guid aggregateId,
             Func<IEventMigration>[] migrationFactories,
             Type eventStoreType)
        {
            var container = new WindsorContainer();

            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            container.Register(
                Component.For<IServiceBus>()
                         .ImplementedBy<SynchronousBus>()
                         .LifestylePerWebRequest(),
                Component.For<IEventStore>()
                         .ImplementedBy(eventStoreType)
                         .DependsOn(Dependency.OnValue<IEnumerable<Func<IEventMigration>>>(migrationFactories))
                         .DependsOn(Dependency.OnValue<string>(ConnectionString))
                         .LifestyleSingleton(),
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>()
                         .ImplementedBy<EventStoreSession>()
                         .LifestyleScoped(),
                Component.For<IWindsorContainer>().Instance(container)
                );

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();

            Console.WriteLine($"Running scenario with {eventStoreType}");

            var initialAggregate = TestAggregate.FromEvents(aggregateId, originalHistory);

            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(initialAggregate));
            var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(initialAggregate.Id)).History;

            var expected = TestAggregate.FromEvents(aggregateId, expectedHistory).History.ToList();

            AssertStreamsAreIdentical(expected, migratedHistory);

            Console.WriteLine("\n\nStreaming all events in store");
            var streamedEvents = container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStore>().StreamEvents().ToList());

            AssertStreamsAreIdentical(expected, streamedEvents);


            Console.WriteLine("\n\nPersisting migrations");
            using(container.BeginScope())
            {
                container.Resolve<IEventStore>().PersistMigrations();
            }


        }

        private static void AssertStreamsAreIdentical(List<IAggregateRootEvent> expected, IReadOnlyList<IAggregateRootEvent> migratedHistory)
        {
            Console.WriteLine($"   Expected: ");
            expected.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine($"\n   Actual: ");
            migratedHistory.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine("\n");

            expected.ForEach(
                (@event, index) =>
                {
                    if(@event.GetType() != migratedHistory[index].GetType())
                    {
                        Assert.Fail(
                            $"Expected event at postion {index} to be of type {@event.GetType()} but it was of type: {migratedHistory[index].GetType()}");
                    }
                });

            migratedHistory.ShouldAllBeEquivalentTo(
                expected,
                config => config.RespectingRuntimeTypes()
                                .WithStrictOrdering()
                                .Excluding(@event => @event.EventId)
                                .Excluding(@event => @event.TimeStamp)
                                .Excluding(@event => @event.InsertionOrder));
        }
    }
}
