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
using Composable.Testing;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    //urgent: Remove this attribute once whole assembly runs all persistence layers.
    [DuplicateByDimensions(nameof(PersistenceLayer.MsSql), nameof(PersistenceLayer.InMemory), nameof(PersistenceLayer.MySql), nameof(PersistenceLayer.PgSql), nameof(PersistenceLayer.Orcl), nameof(PersistenceLayer.DB2))]
    [TestFixture, Performance, LongRunning, Serial]
    public class EventMigrationPerformanceTest : EventMigrationTestBase
    {
        List<AggregateEvent> _history;
        TestAggregate _aggregate;
        IServiceLocator _container;
        IReadOnlyList<IEventMigration> _currentMigrations;
        [OneTimeSetUp] public void Given_a_1000_events_large_aggregate()
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

            _container.ExecuteTransactionInIsolatedScope(() => _container.Resolve<IEventStore>().SaveSingleAggregateEvents(_history));
        }

        [OneTimeTearDown] public void TearDownTask() { _container?.Dispose(); }

        void AssertUncachedAndCachedAggregateLoadTimes(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<IEventMigration> migrations)
        {
            _currentMigrations = migrations;

            void LoadWithCloneLocator(IServiceLocator locator) => locator.ExecuteTransactionInIsolatedScope(() => locator.Resolve<IEventStoreUpdater>()
                                                                                                                         .Get<TestAggregate>(_aggregate.Id));

            IServiceLocator clonedLocator = null;

            TimeAsserter.Execute(
                description: "Uncached loading",
                maxTotal: maxUncachedLoadTime,
                timeFormat: "ss\\.fff",
                setup: () => clonedLocator = _container.Clone(),
                tearDown: () => clonedLocator?.Dispose(),
                action: () => LoadWithCloneLocator(clonedLocator!));

            using(clonedLocator = _container.Clone())
            {
                LoadWithCloneLocator(clonedLocator); //Warm up cache

                TimeAsserter.Execute(
                    description: "Cached loading",
                    maxTotal: maxCachedLoadTime,
                    timeFormat: "ss\\.fff",
                    action: () => LoadWithCloneLocator(clonedLocator));
            }
        }

        //Urgent: Figure out why oracle under performs so dramatically in these tests and fix it. (Hmm. Adding FOR UPDATE to the DB2 query really really slowed DB2 down. Might Oracle be similar?)
        [Test] public void With_four_migrations_mutation_that_all_actually_changes_things_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds_mSSql_25_5_pgSql_25_5_mySql_25_5_orcl_75_5_inMem_15_DB2_25_5()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E2>.Insert<E3>(),
                Before<E4>.Insert<E5>(),
                Before<E6>.Insert<E7>(),
                Before<E8>.Insert<E9>()
            ).ToArray();

            AssertUncachedAndCachedAggregateLoadTimes(
                maxUncachedLoadTime: TestEnv.PersistenceLayer.ValueFor(msSql: 25, mySql: 25, pgSql: 25, orcl: 75, inMem: 15, db2: 25).Milliseconds().InstrumentationSlowdown(2),
                maxCachedLoadTime: TestEnv.PersistenceLayer.ValueFor(msSql: 5, mySql: 5, pgSql: 5, orcl: 5, inMem: 5, db2: 5).Milliseconds().InstrumentationSlowdown(2.5),
                eventMigrations);
        }

        [Test] public void With_four_migrations_that_change_nothing_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_X_milliseconds_mSSql_30_5_pgSql_30_5_mySql_30_5_orcl_100_5_inMem_15_DB2_25_5()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E3>.Insert<E1>(),
                Before<E5>.Insert<E1>(),
                Before<E7>.Insert<E1>(),
                Before<E9>.Insert<E1>()
            ).ToArray();

            AssertUncachedAndCachedAggregateLoadTimes(
                maxUncachedLoadTime: TestEnv.PersistenceLayer.ValueFor(msSql: 30, mySql: 30, pgSql: 30, orcl: 100, inMem: 15, db2: 25).Milliseconds().InstrumentationSlowdown(2),
                maxCachedLoadTime: TestEnv.PersistenceLayer.ValueFor(msSql: 5, mySql: 5, pgSql: 5, orcl: 5, inMem: 5, db2: 5).Milliseconds().InstrumentationSlowdown(2),
                eventMigrations);
        }

        [Test] public void When_there_are_no_migrations_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds_mSSql_20_5_pgSql_20_5_mySql_20_5_orcl_75_5_inMem_10_DB2_25_5()
        {
            var eventMigrations = Seq.Create<IEventMigration>().ToArray();
            AssertUncachedAndCachedAggregateLoadTimes(
                maxUncachedLoadTime: TestEnv.PersistenceLayer.ValueFor(msSql: 20, mySql: 20, pgSql: 20, orcl: 75, inMem: 10, db2: 25).Milliseconds().InstrumentationSlowdown(2),
                maxCachedLoadTime: TestEnv.PersistenceLayer.ValueFor(msSql: 5, mySql: 5, pgSql: 5, orcl: 5, inMem: 5, db2:5).Milliseconds().InstrumentationSlowdown(2.5),
                                                      eventMigrations);
        }
    }
}
