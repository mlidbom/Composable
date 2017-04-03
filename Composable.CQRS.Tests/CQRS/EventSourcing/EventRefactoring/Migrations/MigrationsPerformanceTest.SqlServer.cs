using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations.Events;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture, Performance]
    public class SqlServerMigrationsPerformanceTest : EventStreamMutatorTestsBase
    {
        public SqlServerMigrationsPerformanceTest() : base(typeof(EventStore)) { }

        [Test]
        public void A_ten_thousand_events_large_aggregate_with_four_migrations_should_load_cached_in_less_than_20_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E6>.Insert<E2>(),
                Before<E7>.Insert<E3>(),
                Before<E8>.Insert<E4>(),
                Before<E9>.Insert<E5>()
                ).ToArray();

            using(var serviceLocator = CreateServiceLocatorForEventStoreType(() => eventMigrations, EventStoreType))
            {
                var timeSource = serviceLocator.Resolve<DummyTimeSource>();

                var history = Seq.OfTypes<Ec1>().Concat(1.Through(10000).Select(index => typeof(E1))).ToArray();
                var aggregate = TestAggregate.FromEvents(timeSource, Guid.NewGuid(), history);
                serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>().Save(aggregate));

                //Warm up cache..
                serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>().Get<TestAggregate>(aggregate.Id));

                TimeAsserter.Execute(
                    maxTotal: 20.Milliseconds().AdjustRuntimeToTestEnvironment(),
                    description: "load aggregate in isolated scope",
                    timeFormat: "fff",
                    action: () => serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>().Get<TestAggregate>(aggregate.Id)),
                    maxTries: 3);
            }
        }

      [Test]
      public void A_ten_thousand_events_large_aggregate_with_four_migrations_should_load_uncached_in_less_than_500_milliseconds()
      {
        var eventMigrations =
          Seq.Create<IEventMigration>(
            Before<E6>.Insert<E2>(),
            Before<E7>.Insert<E3>(),
            Before<E8>.Insert<E4>(),
            Before<E9>.Insert<E5>()).ToArray();

        using (var serviceLocator = CreateServiceLocatorForEventStoreType(() => eventMigrations, EventStoreType))
        {
          var timeSource = serviceLocator.Resolve<DummyTimeSource>();

          var history = Seq.OfTypes<Ec1>().Concat(1.Through(10000).Select(index => typeof(E1))).ToArray();
          var aggregate = TestAggregate.FromEvents(timeSource, Guid.NewGuid(), history);
          serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>().Save(aggregate));

          TimeAsserter.Execute(
            maxTotal: 500.Milliseconds().AdjustRuntimeToTestEnvironment(),
            description: "load aggregate in isolated scope",
            action:
            () =>
              serviceLocator.ExecuteInIsolatedScope(
                () => serviceLocator.Resolve<IEventStoreUpdater>().Get<TestAggregate>(aggregate.Id)));
        }
      }

        [Test] public void A_ten_thousand_events_large_aggregate_with_no_migrations_should_load_uncached_in_less_than_300_milliseconds()
        {
            using(var serviceLocator = CreateServiceLocatorForEventStoreType(() => new List<IEventMigration>(), EventStoreType))
            {
                var timeSource = serviceLocator.Resolve<DummyTimeSource>();

                var history = Seq.OfTypes<Ec1>().Concat(1.Through(10000).Select(index => typeof(E1))).ToArray();
                TestAggregate aggregate = null;

                var time = TimeAsserter.Execute(maxTotal: 300.Milliseconds(),
                                                description: "load aggregate in isolated scope",
                                                setup: () =>
                                                       {
                                                           aggregate = TestAggregate.FromEvents(timeSource, Guid.NewGuid(), history);
                                                           serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                                     .Save(aggregate));
                                                       },
                                                action: () => serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                              .Get<TestAggregate>(aggregate.Id)),
                                                maxTries: 10);

                time.Total.Should()
                    .BeGreaterThan(50.Milliseconds(), "It seems we have changed cache behavior and need to refactor this test. We try for speed but this is not likely to ever be achieved uncached.");
            }
        }


    [Test] public void A_ten_thousand_events_large_aggregate_with_no_migrations_should_load_cached_in_less_than_30_milliseconds()
    {
      using (var serviceLocator = CreateServiceLocatorForEventStoreType(() => new List<IEventMigration>(), EventStoreType))
      {
        var timeSource = serviceLocator.Resolve<DummyTimeSource>();

        var history = Seq.OfTypes<Ec1>().Concat(1.Through(10000).Select(index => typeof(E1))).ToArray();
        var aggregate = TestAggregate.FromEvents(timeSource, Guid.NewGuid(), history);
        serviceLocator.ExecuteUnitOfWorkInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>().Save(aggregate));

        TimeAsserter.Execute(
          maxTotal: 30.Milliseconds().AdjustRuntimeToTestEnvironment(),
          description: "load aggregate in isolated scope",
          action:
          () =>
            serviceLocator.ExecuteInIsolatedScope(
              () => serviceLocator.Resolve<IEventStoreUpdater>().Get<TestAggregate>(aggregate.Id)),
          maxTries: 3);
      }
    }


  }
}