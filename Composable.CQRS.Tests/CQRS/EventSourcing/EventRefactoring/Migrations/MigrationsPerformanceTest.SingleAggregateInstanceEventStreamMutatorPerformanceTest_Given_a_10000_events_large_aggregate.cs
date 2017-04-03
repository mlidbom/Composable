using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations.Events;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture, Performance]
    public class SingleAggregateInstanceEventStreamMutatorPerformanceTest_Given_a_10000_events_large_aggregate
    {
        List<AggregateRootEvent> _history;
        TestAggregate _aggregate;
        [SetUp]
        public void Given_a_10000_events_large_aggregate()
        {
            var historyTypes = Seq.OfTypes<Ec1>()
                                  .Concat(
                                      1.Through(10)
                                       .SelectMany(
                                           index => 1.Through(996)
                                                     .Select(_ => typeof(E1))
                                                     .Concat(Seq.OfTypes<E2, E4, E6, E8>()))).ToList();

            _aggregate = TestAggregate.FromEvents(DummyTimeSource.Now, Guid.NewGuid(), historyTypes);
            _history = _aggregate.History.Cast<AggregateRootEvent>().ToList();
        }

        void UseEventstoreSessionWithConfiguredMigrations(Action<IEventStoreUpdater> useSession)
        {

            var container = DependencyInjectionContainer.CreateServiceLocatorForTesting(
                cont => cont.RegisterSqlServerEventStore<ITestingEventstoreUpdater, ITestingEventstoreReader>("dummy connection name"));

            using (container)
            {
                using (container.BeginScope())
                {
                    container.Use<IEventStore<ITestingEventstoreUpdater, ITestingEventstoreReader>>(store => store.SaveEvents(_history));
                }

                using (container.BeginScope())
                {
                    useSession(container.Resolve<ITestingEventstoreUpdater>());
                }
            }
        }

        void AssertAggregateLoadTime(TimeSpan maxTotal)
        {
            UseEventstoreSessionWithConfiguredMigrations
                (session => TimeAsserter.Execute(
                                                 maxTotal: maxTotal,
                                                 description: "load aggregate in isolated scope",
                                                 maxTries: 10,
                                                 timeFormat: "ss\\.fff",
                                                 action: () => session.Get<TestAggregate>(_aggregate.Id)));
        }

        [Test]
        public void Aggregate_should_raise_100_000_events_in_less_than_180_milliseconds()
        {
            var history = Seq.OfTypes<Ec1>()
                                  .Concat(1.Through(10000).Select(_ => typeof(E1)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E2)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E3)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E4)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E5)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E6)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E7)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E8)))
                                  .Concat(1.Through(10000).Select(_ => typeof(E9)))
                                  .Concat(1.Through(10000).Select(_ => typeof(Ef)))
                             .ToEvents();

            TimeAsserter.Execute(
                maxTotal: 180.Milliseconds().AdjustRuntimeToTestEnvironment(),
                // ReSharper disable once ObjectCreationAsStatement
                action: () => new TestAggregate2(history),
                maxTries: 10);
        }

        [Test]
        public void With_four_migrations_mutation_that_all_actually_changes_things_loading_takes_less_than_15_milliseconds()
        {
            AssertAggregateLoadTime(15.Milliseconds().AdjustRuntimeToTestEnvironment());
        }

        [Test]
        public void With_four_migrations_that_change_nothing_loading_takes_less_than_10_milliseconds()
        {
            AssertAggregateLoadTime(10.Milliseconds().AdjustRuntimeToTestEnvironment());
        }

        [Test]
        public void Calling_before_after_or_replace_100_000_times_takes_less_than_60_milliseconds()
        {
            var before = Before<E3>.Insert<E2>().CreateSingleAggregateInstanceHandlingMigrator();
            var replace = Replace<E3>.With<E2>().CreateSingleAggregateInstanceHandlingMigrator();
            var after = After<E3>.Insert<E2>().CreateSingleAggregateInstanceHandlingMigrator();
            var @event = new E2();
            var eventModifier = new DummyEventModifier();

            var numberOfEventsToInspect = 100000;
            var maxtime = 60.Milliseconds().AdjustRuntimeToTestEnvironment();

            TimeAsserter.Execute(
                maxTotal: maxtime,
                description: $"{nameof(before)}",
                iterations: numberOfEventsToInspect,
                maxTries:3,
                action: () => before.MigrateEvent(@event, eventModifier));
            TimeAsserter.Execute(
                maxTotal: maxtime,
                maxTries: 3,
                description: $"{nameof(replace)}",
                iterations: numberOfEventsToInspect,
                action: () => replace.MigrateEvent(@event, eventModifier));
            TimeAsserter.Execute(
                maxTotal: maxtime,
                maxTries: 3,
                description: $"{nameof(after)}",
                iterations: numberOfEventsToInspect,
                action: () => after.MigrateEvent(@event, eventModifier));
        }

        [Test]
        public void When_there_are_no_migrations_mutation_takes_less_than_a_millisecond()
        {
            AssertAggregateLoadTime(1.Milliseconds());

        }

        class DummyEventModifier : IEventModifier
        {
            public void Replace(params AggregateRootEvent[] events) { throw new NotImplementedException(); }
            public void InsertBefore(params AggregateRootEvent[] insert) { throw new NotImplementedException(); }
        }
    }

    class TestAggregate2 : AggregateRoot<TestAggregate, RootEvent, IRootEvent>
    {
        void RaiseEvents(params RootEvent[] events)
        {
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateRootId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<AggregateRootEvent>().First().AggregateRootId = Id;
            }

            foreach (var @event in events)
            {
                RaiseEvent(@event);
            }
        }

        TestAggregate2():base(new DateTimeNowTimeSource())
        {
            RegisterEventAppliers()
                .For<IRootEvent>(e => {})
                .For<Ec1>(e => { })
                .For<Ec2>(e => { })
                .For<Ec3>(e => { })
                .For<E1>(e => { })
                .For<E2>(e => { })
                .For<E3>(e => { })
                .For<E4>(e => { })
                .For<E5>(e => { })
                .For<E6>(e => { })
                .For<E7>(e => { })
                .For<E8>(e => { })
                .For<E9>(e => { })
                .For<Ef>(e => { });
        }

        public TestAggregate2(params RootEvent[] events) : this()
        {
            Contract.Assert.That(events.First() is IAggregateRootCreatedEvent, "events.First() is IAggregateRootCreatedEvent");

            RaiseEvents(events);
        }
    }
}
