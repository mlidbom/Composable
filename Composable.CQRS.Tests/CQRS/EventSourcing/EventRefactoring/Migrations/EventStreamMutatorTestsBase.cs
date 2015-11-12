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
        protected void RunMigrationTest
            (
            IEnumerable<Type> originalHistory,
            IEnumerable<Type> expectedHistory,
            params IEventMigration[] manualMigrations)
        {
            var migrationInstances = manualMigrations;
            var aggregateId = Guid.NewGuid();

            RunScenarioWithEventStoreType(originalHistory, expectedHistory, manualMigrations, aggregateId, migrationInstances, typeof(InMemoryEventStore));
            //RunScenarioWithEventStoreType(originalHistory, expectedHistory, manualMigrations, aggregateId, migrationInstances, typeof(SqlServerEventStore));
        }

        private static void RunScenarioWithEventStoreType
            (IEnumerable<Type> originalHistory,
             IEnumerable<Type> expectedHistory,
             IEventMigration[] manualMigrations,
             Guid aggregateId,
             IEventMigration[] migrationInstances,
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
                         .DependsOn(Dependency.OnValue<IEnumerable<IEventMigration>>(manualMigrations))
                         .DependsOn(Dependency.OnValue<string>(ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString))
                         .LifestyleSingleton(),
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>()
                         .ImplementedBy<EventStoreSession>()
                         .LifestyleScoped(),
                Component.For<IWindsorContainer>().Instance(container)
                );

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();

            Console.WriteLine($"Running scenario with {eventStoreType}");

            var aggregate = TestAggregate.FromEvents(aggregateId, originalHistory);

            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(aggregate));
            container.ExecuteUnitOfWorkInIsolatedScope(() => aggregate = container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id));


            var mutatedHistory = new SingleAggregateEventStreamMutator(aggregate.Id, migrationInstances)
                .MutateCompleteAggregateHistory(aggregate.History).ToList();

            var expected = TestAggregate.FromEvents(aggregateId, expectedHistory).History.ToList();

            Console.WriteLine($"   Expected: ");
            expected.ForEach(e => Console.WriteLine($"      {e}"));
            Console.WriteLine($"\n   Actual: ");
            mutatedHistory.ForEach(e => Console.WriteLine($"      {e}"));

            expected.ForEach(
                (@event, index) =>
                {
                    if (@event.GetType() != mutatedHistory[index].GetType())
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
                                .Excluding(@event => @event.TimeStamp)
                                .Excluding(@event => @event.InsertionOrder));
        }

    }
}
