using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations.Events;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.DependencyInjection.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations {
    [TestFixture]
    public class EventMigrationTest_SqlServer : EventMigrationTest
    {
        public EventMigrationTest_SqlServer() : base(typeof(EventStore)) { }

        [Test]
        public void Persisting_migrations_and_then_updating_the_aggregate_from_another_processes_EventStore_results_in_both_processes_seeing_identical_histories()
        {
            var actualMigrations = Seq.Create(Replace<E1>.With<E2>()).ToArray();
            IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();

            // ReSharper disable once AccessToModifiedClosure this is exactly what we wish to achieve here...
            using (var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations, EventStoreType))
            {
                IEventStore<ITestingEventstoreUpdater, ITestingEventstoreReader> PersistingEventStore() => serviceLocator.Resolve<IEventStore<ITestingEventstoreUpdater, ITestingEventstoreReader>>();

                using (var otherProcessServiceLocator = serviceLocator.Clone())
                {
                    // ReSharper disable once AccessToDisposedClosure
                    ITestingEventstoreUpdater OtherEventstoreSession() => otherProcessServiceLocator.Resolve<ITestingEventstoreUpdater>();

                    var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

                    var aggregate = TestAggregate.FromEvents(
                        serviceLocator.Resolve<IUtcTimeTimeSource>(),
                        id,
                        Seq.OfTypes<Ec1, E1, E2, E3, E4>());

                    otherProcessServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() => OtherEventstoreSession().Save(aggregate));
                    migrations = actualMigrations;
                    otherProcessServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() => OtherEventstoreSession().Get<TestAggregate>(id));

                    var test = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => PersistingEventStore().GetAggregateHistory(id));
                    test.Count.Should().BeGreaterThan(0);

                    serviceLocator.ExecuteInIsolatedScope(() => PersistingEventStore().PersistMigrations());

                    otherProcessServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() => OtherEventstoreSession().Get<TestAggregate>(id).RaiseEvents(new E3()));

                    var firstProcessHistory = serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => PersistingEventStore().GetAggregateHistory(id));
                    var secondProcessHistory = otherProcessServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() => otherProcessServiceLocator.Resolve<IEventStore<ITestingEventstoreUpdater, ITestingEventstoreReader>>().GetAggregateHistory(id));

                    EventMigrationTestBase.AssertStreamsAreIdentical(firstProcessHistory, secondProcessHistory, "Both process histories should be identical");

                }
            }
        }
    }
}