using System;
using System.Linq;
using Composable.CQRS.EventSourcing;
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
    public class InMemoryMigrationsPerformanceTest : EventStreamMutatorTestsBase
    {
        public InMemoryMigrationsPerformanceTest() : base(typeof(InMemoryEventStore)) { }

        [Test]//Do not worry about it if this test fails when running in ncrunch. It runs it much slower for some reason. Probably due to intrumenting the assembly. Just ignore it in ncrunch.
        public void A_hundred_thousand_events_large_aggregate_with_four_migrations_should_load_cached_in_less_than_150_milliseconds()
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

                var history = Seq.OfTypes<Ec1>().Concat(1.Through(100000).Select(index => typeof(E1))).ToArray();
                var aggregate = TestAggregate.FromEvents(timeSource, Guid.NewGuid(), history);
                container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Save(aggregate));

                //Warm up cache..
                container.ExecuteUnitOfWorkInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id));

                TimeAsserter.Execute(
                    maxTotal: 150.Milliseconds().AdjustRuntimeToTestEnvironment(boost:3),
                    action: () => container.ExecuteInIsolatedScope(() => container.Resolve<IEventStoreSession>().Get<TestAggregate>(aggregate.Id))
                    , maxTries: 10);
            }
        }
    }
}
