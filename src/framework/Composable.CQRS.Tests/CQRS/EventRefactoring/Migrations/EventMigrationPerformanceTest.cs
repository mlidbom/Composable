using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;
using Composable.Testing.Performance;
using Composable.Tests.CQRS.EventRefactoring.Migrations.Events;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    //urgent: Remove this attribute once whole assembly runs all persistence layers.
    [DuplicateByDimensions(nameof(PersistenceLayer.SqlServer), nameof(PersistenceLayer.InMemory))]
    [TestFixture, Performance, LongRunning, Serial]
    public class EventMigrationPerformanceTest : EventMigrationTestBase
    {
        List<AggregateEvent> _history;
        TestAggregate _aggregate;
        IServiceLocator _container;
        IReadOnlyList<IEventMigration> _currentMigrations;
        [OneTimeSetUp]
        public void Given_a_1000_events_large_aggregate()
        {
            var historyTypes = Seq.OfTypes<Ec1>()
                                  .Concat(
                                      1.Through(10)
                                       .SelectMany(
                                           index => 1.Through(96)
                                                     .Select(_ => typeof(E1))
                                                     .Concat(Seq.OfTypes<E2, E4, E6, E8>()))).ToList();

            _aggregate = TestAggregate.FromEvents(TestingTimeSource.FrozenUtcNow(), Guid.NewGuid(), historyTypes);
            _history = _aggregate.History.Cast<AggregateEvent>().ToList();

            _currentMigrations = Seq.Empty<IEventMigration>().ToList();
            _container = CreateServiceLocatorForEventStoreType(migrationsfactory: () => _currentMigrations);

            _container.ExecuteTransactionInIsolatedScope(()=> _container.Resolve<IEventStore>().SaveSingleAggregateEvents(_history));
        }

        [OneTimeTearDown] public void TearDownTask() { _container?.Dispose(); }


        void AssertUncachedAggregateLoadTime(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<IEventMigration> migrations)
        {
                _currentMigrations = migrations;


            void LoadWithCloneLocator(IServiceLocator locator) => locator.ExecuteTransactionInIsolatedScope(() => locator.Resolve<IEventStoreUpdater>()
                                                                                                                         .Get<TestAggregate>(_aggregate.Id));

            IServiceLocator clonedLocator = null;

            TimeAsserter.Execute(
                    maxTotal: maxUncachedLoadTime,
                    timeFormat: "ss\\.fff",
                    setup: () => clonedLocator = _container.Clone(),
                    tearDown: () => clonedLocator?.Dispose(),
                    action: () => LoadWithCloneLocator(clonedLocator!));

            using(clonedLocator = _container.Clone())
            {
                LoadWithCloneLocator(clonedLocator);//Warm up cache

                TimeAsserter.Execute(
                    maxTotal: maxCachedLoadTime,
                    timeFormat: "ss\\.fff",
                    action: () => LoadWithCloneLocator(clonedLocator));
            }
        }

        [Test]
        public void With_four_migrations_mutation_that_all_actually_changes_things_uncached_loading_takes_less_than_20_milliseconds_cached_less_than_5_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E2>.Insert<E3>()
                ,Before<E4>.Insert<E5>()
                ,Before<E6>.Insert<E7>()
                ,Before<E8>.Insert<E9>()
            ).ToArray();

            AssertUncachedAggregateLoadTime(20.Milliseconds().InstrumentationSlowdown(2), 5.Milliseconds().InstrumentationSlowdown(2.5), eventMigrations);
        }

        [Test]
        public void With_four_migrations_that_change_nothing_uncached_loading_takes_less_than_20_milliseconds_cached_less_than_5_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E3>.Insert<E1>(),
                Before<E5>.Insert<E1>(),
                Before<E7>.Insert<E1>(),
                Before<E9>.Insert<E1>()
            ).ToArray();

            AssertUncachedAggregateLoadTime(20.Milliseconds().InstrumentationSlowdown(2), 5.Milliseconds().InstrumentationSlowdown(2), eventMigrations);
        }

        [Test]
        public void When_there_are_no_migrations_uncached_loading_takes_less_than_20_milliseconds_cached_less_than_5_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>().ToArray();
            AssertUncachedAggregateLoadTime(20.Milliseconds().InstrumentationSlowdown(2), 5.Milliseconds().InstrumentationSlowdown(2), eventMigrations);

        }
    }
}
