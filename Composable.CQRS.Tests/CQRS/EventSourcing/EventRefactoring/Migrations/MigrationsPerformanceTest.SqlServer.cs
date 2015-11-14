using System;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.Windsor;
using Composable.System.Linq;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public class SqlServerMigrationsPerformanceTest : EventStreamMutatorTestsBase
    {
        public SqlServerMigrationsPerformanceTest() : base(typeof(SqlServerEventStore)) { }

        [Test]//Do not worry about it if this test fails when running in ncrunch. It runs it much slower for some reason. Probably due to intrumenting the assembly. Just ignore it in ncrunch.
        public void A_ten_thousand_events_large_aggregate_with_four_migrations_should_load_cached_in_less_than_50_milliseconds()
        {
            var eventMigrations = Seq.Create<IEventMigration>(
                Before<E6>.Insert<E2>(),
                Before<E7>.Insert<E3>(),
                Before<E8>.Insert<E4>(),
                Before<E9>.Insert<E5>()
                ).ToArray();

            var container = CreateContainerForEventStoreType(eventMigrations, EventStoreType);

            var history = Seq.OfTypes<Ec1>().Concat(1.Through(10000).Select(index => typeof(E1))).ToArray();
            var aggregate = TestAggregate.FromEvents(Guid.NewGuid(), history);
            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(aggregate));

            //Warm up cache..
            container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id));

            TimeAsserter.Execute(
                maxAverage: 50.Milliseconds(), iterations: 10, description: "load aggregate in isolated scope", timeFormat: "fff",
                action: () => container.ExecuteInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id)));
        }
    }
}