﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.DependencyInjection.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;
using Composable.Testing.Performance;
using Composable.Tests.CQRS.EventRefactoring.Migrations.Events;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using Composable.System;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TestFixture, Performance, LongRunning, Serial]
    public abstract class EventMigrationPerformanceTest : EventMigrationTestBase
    {
        public EventMigrationPerformanceTest(Type eventStoreType) : base(eventStoreType) { }

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
            _container = CreateServiceLocatorForEventStoreType(migrationsfactory: () => _currentMigrations, eventStoreType: EventStoreType);

            _container.ExecuteTransactionInIsolatedScope(()=> _container.Resolve<IEventStore<ITestingEventStoreUpdater, ITestingEventStoreReader>>().SaveEvents(_history));
        }

        [OneTimeTearDown] public void TearDownTask() { _container?.Dispose(); }


        void AssertUncachedAggregateLoadTime(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<IEventMigration> migrations)
        {
                _currentMigrations = migrations;

                IServiceLocator clonedLocator = null;

            void LoadWithCloneLocator() => clonedLocator.ExecuteTransactionInIsolatedScope(() => clonedLocator.Resolve<ITestingEventStoreUpdater>()
                                                                                                   .Get<TestAggregate>(_aggregate.Id));
                TimeAsserter.Execute(
                    maxTotal: maxUncachedLoadTime,
                    timeFormat: "ss\\.fff",
                    setup: () => clonedLocator = _container.Clone(),
                    tearDown: () => clonedLocator?.Dispose(),
                    action: LoadWithCloneLocator);

            using(clonedLocator = _container.Clone())
            {
                LoadWithCloneLocator();//Warm up cache

                TimeAsserter.Execute(
                    maxTotal: maxCachedLoadTime,
                    timeFormat: "ss\\.fff",
                    action: LoadWithCloneLocator);
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
