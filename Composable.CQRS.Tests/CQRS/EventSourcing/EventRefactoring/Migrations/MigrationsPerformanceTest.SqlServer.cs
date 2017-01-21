using System;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.GenericAbstractions.Time;
using Composable.System.Linq;
using Composable.Testing;
using Composable.Windsor;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class SqlServerMigrationsPerformanceTest : EventStreamMutatorTestsBase
    {
        public SqlServerMigrationsPerformanceTest() : base(typeof(SqlServerEventStore)) { }

        [Test]
        public void A_ten_thousand_events_large_aggregate_with_four_migrations_should_load_cached_in_less_than_20_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E6>.Insert<E2>(),
                Before<E7>.Insert<E3>(),
                Before<E8>.Insert<E4>(),
                Before<E9>.Insert<E5>()
                ).ToArray();

            using(var container = CreateContainerForEventStoreType(() => eventMigrations, EventStoreType))
            {
                var timeSource = container.Resolve<DummyTimeSource>();

                var history = Seq.OfTypes<Ec1>().Concat(1.Through(10000).Select(index => typeof(E1))).ToArray();
                var aggregate = TestAggregate.FromEvents(timeSource, Guid.NewGuid(), history);
                container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(aggregate));

                //Warm up cache..
                container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id));

                TimeAsserter.Execute(
                    maxTotal: 20.Milliseconds().AdjustRuntimeForNCrunch(),
                    description: "load aggregate in isolated scope",
                    timeFormat: "fff",
                    action: () => container.ExecuteInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id)));
            }
        }
    }
}