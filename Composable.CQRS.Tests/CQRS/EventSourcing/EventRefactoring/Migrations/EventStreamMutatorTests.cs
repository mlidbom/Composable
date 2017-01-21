using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.GenericAbstractions.Time;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.Windsor;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;
using TestAggregates.Events;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class InMemoryEventStoreEventStreamMutatorTests : EventStreamMutatorTests
    {
        public InMemoryEventStoreEventStreamMutatorTests() : base(typeof(InMemoryEventStore)) { }
    }

    public class SqlServerEventStoreEventStreamMutatorTests : EventStreamMutatorTests
    {
        public SqlServerEventStoreEventStreamMutatorTests() : base(typeof(SqlServerEventStore)) { }

        [Test]
        public void PersistingMigrationsAndThenUpdatingTheAggregateFromAnotherProcessesEventStore()
        {
            var actualMigrations = Seq.Create(Replace<E1>.With<E2>()).ToArray();
            IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();

            using (var persistingContainer = CreateContainerForEventStoreType(() => migrations, EventStoreType))
            {
                string eventStoreConnectionString;
                using(persistingContainer.BeginScope())
                {
                    eventStoreConnectionString = ((SqlServerEventStore)persistingContainer.Resolve<IEventStore>()).ConnectionString;
                    eventStoreConnectionString = eventStoreConnectionString + ";";
                }
                Func<IEventStore> persistingEventStore = () => persistingContainer.Resolve<IEventStore>();

                using (var otherProcessContainer = CreateContainerForEventStoreType(() => migrations, EventStoreType, eventStoreConnectionString))
                {
                    Func<IEventStoreSession> otherEventstoreSession = () => otherProcessContainer.Resolve<IEventStoreSession>();

                    var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

                    var aggregate = TestAggregate.FromEvents(
                        persistingContainer.Resolve<IUtcTimeTimeSource>(),
                        id,
                        Seq.OfTypes<Ec1, E1, E2, E3, E4>());

                    otherProcessContainer.ExecuteUnitOfWorkInIsolatedScope(() => otherEventstoreSession().Save(aggregate));
                    migrations = actualMigrations;
                    otherProcessContainer.ExecuteUnitOfWorkInIsolatedScope(() => otherEventstoreSession().Get<TestAggregate>(id));

                    var test = persistingContainer.ExecuteUnitOfWorkInIsolatedScope(() => persistingEventStore().GetAggregateHistory(id));
                    test.Count().Should().BeGreaterThan(0);

                    persistingContainer.ExecuteInIsolatedScope(() => persistingEventStore().PersistMigrations());

                    otherProcessContainer.ExecuteUnitOfWorkInIsolatedScope(
                        () =>
                        {
                            otherEventstoreSession().Get<TestAggregate>(id).RaiseEvents(new E3());
                        });

                }
            }
        }
    }

    [TestFixture]
    public abstract class EventStreamMutatorTests : EventStreamMutatorTestsBase
    {
        protected EventStreamMutatorTests(Type eventStoreType) : base(eventStoreType) { }

        [Test]
        public void Base_class_method_should_detect_incorrect_type_order()
        {
            this.Invoking(
                _ => RunMigrationTest(
                    new MigrationScenario(
                        Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                        Seq.OfTypes<Ec1, Ef, E2, Ef>())))
                .ShouldThrow<Exception>();
        }

        [Test]
        public void Replacing_E1_with_E2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef, Ef>(),
            Seq.OfTypes<Ec1, E2, Ef, Ef>(),
            Replace<E1>.With<E2>()));
        }

        [Test]
        public void Replacing_E1_with_E2_at_end_of_stream()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1>(),
            Seq.OfTypes<Ec1, E2>(),
            Replace<E1>.With<E2>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3_at_end_of_stream()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1>(),
            Seq.OfTypes<Ec1, E2, E3>(),
            Replace<E1>.With<E2, E3>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E2, E3, Ef>(),
            Replace<E1>.With<E2, E3>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3_2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef, Ef, Ef, Ef>(),
            Seq.OfTypes<Ec1, E2, E3, Ef, Ef, Ef, Ef>(),
            Replace<E1>.With<E2, E3>()));
        }

        [Test]
        public void Replacing_E1_with_E2_then_irrelevant_migration()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E2, Ef>(),
            Replace<E1>.With<E2>(),
            Replace<E1>.With<E5>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E2, E3, Ef>(),
            Replace<E1>.With<E2, E3>(),
            Replace<E1>.With<E5>()));
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_E2_with_E4()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E4, E3, Ef>(),
            Replace<E1>.With<E2, E3>(),//Ec1, E2, E3, Ef
            Replace<E2>.With<E4>())); //Ec1, E4, E3, Ef
        }

        [Test]
        public void Inserting_E3_before_E1()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E3, E1, Ef>(),
            Before<E1>.Insert<E3>()));
        }

        [Test]
        public void Inserting_E3_E4_before_E1()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E3, E4, E1, Ef>(),
            Before<E1>.Insert<E3, E4>()));
        }

        [Test]
        public void Inserting_E2_before_E1_then_E3_before_E2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E3, E2, E1, Ef>(),
            Before<E1>.Insert<E2>(),
            Before<E2>.Insert<E3>()));
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1>(),
            Seq.OfTypes<Ec1, E5, E3, E4, E1>(),
            Before<E1>.Insert<E3, E4>(),//Ec1, E3, E4, E1
            Before<E3>.Insert<E5>())); //Ec1, E5, E3, E4, E1;
        }

        [Test]
        public void Given_Ec1_E1_Ef_Inserting_E3_E4_before_E1_then_E5_before_E4()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E3, E5, E4, E1, Ef>(),
            Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef
            Before<E4>.Insert<E5>())); //Ec1, E3, E5, E4, E1, Ef
        }

        [Test]
        public void Given_Ec1_E1_Inserting_E2_before_E1_then_E3_before_E2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1>(),
            Seq.OfTypes<Ec1, E3, E2, E1>(),
            Before<E1>.Insert<E2>(), //Ec1, E2, E1
            Before<E2>.Insert<E3>())); //Ec1, E3, E2, E1
        }

        [Test]
        public void Given_Ec1_E1_Inserting_E3_E2_before_E1_then_E4_before_E3()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1>(),
            Seq.OfTypes<Ec1, E5, E4, E3, E2, E1>(),
            Before<E1>.Insert<E3, E2>(), //Ec1, E3, E2, E1
            Before<E3>.Insert<E4>(),
            Before<E4>.Insert<E5>())); //Ec1, E4, E3, E2, E1
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef>(),
            Seq.OfTypes<Ec1, E6, E5, E4, E1, Ef>(),
            Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef
            Before<E4>.Insert<E5>(), //Ec1, E3, E5, E4, E1, Ef
            Replace<E3>.With<E6>())); //Ec1, E6, E5, E4, E1, Ef
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6_then_replace_Ef_with_E7_then_insert_E8_after_E7()
        {
            RunMigrationTest(new MigrationScenario
                (Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E6, E5, E4, E1, E7, E8>(),
                Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef
                Before<E4>.Insert<E5>(), //Ec1, E3, E5, E4, E1, Ef
                Replace<E3>.With<E6>(), //Ec1, E6, E5, E4, E1, Ef
                Replace<Ef>.With<E7>(), //Ec1, E6, E5, E4, E1, E7
                After<E7>.Insert<E8>())); //Ec1, E6, E5, E4, E1, E7, E8
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3_2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef, Ef>(),
            Seq.OfTypes<Ec1, E5, E3, E4, E1, Ef, Ef>(),
            Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef, Ef
            Before<E3>.Insert<E5>())); //Ec1, E5, E3, E4, E1, Ef, Ef
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_2()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef, Ef>(),
            Seq.OfTypes<Ec1, E3, E5, E4, E1, Ef, Ef>(),
            Before<E1>.Insert<E3, E4>(),//Ec1, E3, E4 E1, Ef, Ef
            Before<E4>.Insert<E5>())); //Ec1, E3, E5, E4, E1, Ef, Ef
        }

        [Test]
        public void Inserting_E2_after_E1()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1, Ef, Ef>(),
            Seq.OfTypes<Ec1, E1, E2, Ef, Ef>(),
            After<E1>.Insert<E2>()));

        }

        [Test]
        public void Inserting_E2_after_E1_at_end_of_stream()
        {
            RunMigrationTest(new MigrationScenario(
            Seq.OfTypes<Ec1, E1>(),
            Seq.OfTypes<Ec1, E1, E2>(),
            After<E1>.Insert<E2>()));
        }

        [Test]
        public void Given_Ec1_E1_before_E1_E2_after_E2_E3_throws_NonIdempotentMigrationDetectedException()
        {
            this.Invoking(
                me =>
                RunMigrationTest(
                    new MigrationScenario(
                        Seq.OfTypes<Ec1, E1>(),
                        Seq.OfTypes<Ec1, E2, E3, E1>(),
                        Before<E1>.Insert<E2>(),
                        After<E2>.Insert<E3>())))
                .ShouldThrow<NonIdempotentMigrationDetectedException>();
        }

        [Test]
        public void PersistingMigrationsOfTheSameAggregateMultipleTimes()
        {
            var emptyMigrationsArray = new IEventMigration[0];
            IReadOnlyList<IEventMigration> migrations = emptyMigrationsArray;

            using(var container = CreateContainerForEventStoreType(() => migrations, EventStoreType))
            {

                var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

                container.Resolve<DummyTimeSource>().UtcNow = DateTime.Parse("2001-01-01 12:00");

                var aggregate = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1, E2, E3, E4>());
                var initialHistory = aggregate.History;


                Func<IEventStoreSession> session = () => container.Resolve<IEventStoreSession>();
                Func<IEventStore> eventStore = () => container.Resolve<IEventStore>();

                var firstSavedHistory = container.ExecuteUnitOfWorkInIsolatedScope(
                    () =>
                    {
                        session().Save(aggregate);
                        return session().Get<TestAggregate>(id).History;
                    });


                AssertStreamsAreIdentical(initialHistory, firstSavedHistory, "first saved history");

                migrations = Seq.Create(Replace<E1>.With<E5>()).ToList();

                var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                var expectedAfterReplacingE1WithE5 =
                    TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E5, E2, E3, E4>()).History;
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

                var historyAfterPersistingButBeforeReload = container.ExecuteUnitOfWorkInIsolatedScope(
                    () =>
                    {
                        eventStore().PersistMigrations();
                        return session().Get<TestAggregate>(id).History;
                    });

                AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");

                var historyAfterPersistingAndReloading = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

                migrations = Seq.Create(Replace<E2>.With<E6>()).ToList();
                ClearEventstoreCache(container);

                migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                var expectedAfterReplacingE2WithE6 = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E5, E6, E3, E4>()).History;
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

                historyAfterPersistingButBeforeReload = container.ExecuteUnitOfWorkInIsolatedScope(
                    () =>
                    {
                        eventStore().PersistMigrations();
                        return session().Get<TestAggregate>(id).History;
                    });

                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");
                historyAfterPersistingAndReloading = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");
            }

        }
        

        [Test]
        public void PersistingMigrationsOfTheSameAggregateMultipleTimesWithEventsAddedInTheMiddleAndAfter()
        {
            var emptyMigrationsArray = new IEventMigration[0];
            IReadOnlyList<IEventMigration> migrations = emptyMigrationsArray;

            using (var container = CreateContainerForEventStoreType(() => migrations, EventStoreType))
            {

                var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

                container.Resolve<DummyTimeSource>().UtcNow = DateTime.Parse("2001-01-01 12:00");

                var aggregate = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1, E2, E3, E4>());
                var initialHistory = aggregate.History;


                Func<IEventStoreSession> session = () => container.Resolve<IEventStoreSession>();
                Func<IEventStore> eventStore = () => container.Resolve<IEventStore>();

                var firstSavedHistory = container.ExecuteUnitOfWorkInIsolatedScope(
                    () =>
                    {
                        session().Save(aggregate);
                        return session().Get<TestAggregate>(id).History;
                    });


                AssertStreamsAreIdentical(initialHistory, firstSavedHistory, "first saved history");

                migrations = Seq.Create(Replace<E1>.With<E5>()).ToList();

                var migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                var expectedAfterReplacingE1WithE5 =
                    TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E5, E2, E3, E4>()).History;
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

                var historyAfterPersistingButBeforeReload = container.ExecuteUnitOfWorkInIsolatedScope(
                    () =>
                    {
                        eventStore().PersistMigrations();
                        return session().Get<TestAggregate>(id).History;
                    });

                AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");

                var historyAfterPersistingAndReloading = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

                container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).RaiseEvents(new E6(), new E7()));

                migrations = Seq.Create(Replace<E2>.With<E6>()).ToList();
                ClearEventstoreCache(container);

                migratedHistory = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                var expectedAfterReplacingE2WithE6 = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E5, E6, E3, E4, E6, E7>()).History;
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

                historyAfterPersistingButBeforeReload = container.ExecuteUnitOfWorkInIsolatedScope(
                    () =>
                    {
                        eventStore().PersistMigrations();
                        return session().Get<TestAggregate>(id).History;
                    });

                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");
                historyAfterPersistingAndReloading = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

                migrations = Seq.Empty<IEventMigration>().ToList();
                container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).RaiseEvents(new E8(), new E9()));
                historyAfterPersistingAndReloading = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                var expectedAfterReplacingE2WithE6AndRaisingE8E9 = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E5, E6, E3, E4, E6, E7, E8, E9>()).History;
                AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6AndRaisingE8E9, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

            }

        }

        [Test]
        public void UpdatingAnAggregateAfterPersistingMigrations()
        {
            var emptyMigrationsArray = new IEventMigration[0];
            IReadOnlyList<IEventMigration> migrations = emptyMigrationsArray;

            using(var container = CreateContainerForEventStoreType(() => migrations, EventStoreType))
            {

                var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

                container.Resolve<DummyTimeSource>().UtcNow = DateTime.Parse("2001-01-01 12:00");

                var initialAggregate = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1>());
                var initialHistory = initialAggregate.History;


                Func<IEventStoreSession> session = () => container.Resolve<IEventStoreSession>();
                Func<IEventStore> eventStore = () => container.Resolve<IEventStore>();

                container.ExecuteUnitOfWorkInIsolatedScope(() => session().Save(initialAggregate));

                migrations = Seq.Create(Replace<E1>.With<E5>()).ToList();

                container.ExecuteUnitOfWorkInIsolatedScope(() => eventStore().PersistMigrations());

                migrations = Seq.Create<IEventMigration>().ToList();

                container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).RaiseEvents(new E2()));

                var aggregate = container.ExecuteInIsolatedScope(() => session().Get<TestAggregate>(id));

                var expected = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E5, E2>()).History;
                AssertStreamsAreIdentical(expected: expected, migratedHistory: aggregate.History, descriptionOfHistory: "migrated history");

                var completeEventHistory =container.ExecuteInIsolatedScope(() => eventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()).Cast<AggregateRootEvent>();
                AssertStreamsAreIdentical(expected: expected, migratedHistory: completeEventHistory, descriptionOfHistory: "streamed persisted history");

                Console.WriteLine($"Version");
                Console.WriteLine("Aggregate Effective Inserted Manual");
                Console.WriteLine("A E I M");
                completeEventHistory.ForEach(@event => Console.WriteLine($"{@event.AggregateRootVersion} {@event.EffectiveVersion} {@event.InsertedVersion} {@event.ManualVersion}"));

                ClearEventstoreCache(container);

                completeEventHistory = container.ExecuteInIsolatedScope(() => eventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()).Cast<AggregateRootEvent>();
                AssertStreamsAreIdentical(expected: expected, migratedHistory: completeEventHistory, descriptionOfHistory: "streamed persisted history");
            }
        }

        [Test]
        public void Inserting_E2_Before_E1_Persisting_and_then_Inserting_E3_before_E1()
        {
            var firstMigration = Seq.Create(Before<E1>.Insert<E2>()).ToArray();
            var secondMigration = Seq.Create(Before<E1>.Insert<E3>()).ToArray();
            IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();
            using (var container = CreateContainerForEventStoreType(() => migrations, EventStoreType))
            {
                container.Resolve<DummyTimeSource>().UtcNow = DateTime.Parse("2001-01-01 12:00");

                var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
                var initialAggregate = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1>());
                var expectedHistoryAfterFirstMigration = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E2, E1>()).History;
                var expectedHistoryAfterSecondMigration = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E2, E3, E1>()).History;

                Func<IEventStoreSession> session = () => container.Resolve<IEventStoreSession>();
                Func<IEventStore> eventStore = () => container.Resolve<IEventStore>();                

                container.ExecuteUnitOfWorkInIsolatedScope(() => session().Save(initialAggregate));
                migrations = firstMigration;
                var historyWithFirstMigrationUnPersisted = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);

                container.ExecuteUnitOfWorkInIsolatedScope(() => eventStore().PersistMigrations());
                var historyAfterPersistingFirstMigration = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyWithFirstMigrationUnPersisted, nameof(historyWithFirstMigrationUnPersisted));
                AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyAfterPersistingFirstMigration, nameof(historyAfterPersistingFirstMigration));

                migrations = secondMigration;
                ClearEventstoreCache(container);
                var historyWithSecondMigrationUnPersisted = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);

                container.ExecuteUnitOfWorkInIsolatedScope(() => eventStore().PersistMigrations());
                var historyAfterPersistingSecondMigration = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);                
                AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyWithSecondMigrationUnPersisted, nameof(historyWithSecondMigrationUnPersisted));
                AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyAfterPersistingSecondMigration, nameof(historyAfterPersistingSecondMigration));
            }
        }

        [Test]
        public void Inserting_E2_After_E1_Persisting_and_then_Inserting_E3_after_E1()
        {
            var firstMigration = Seq.Create(After<E1>.Insert<E2>()).ToArray();
            var secondMigration = Seq.Create(After<E1>.Insert<E3>()).ToArray();
            IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();
            using (var container = CreateContainerForEventStoreType(() => migrations, EventStoreType))
            {
                container.Resolve<DummyTimeSource>().UtcNow = DateTime.Parse("2001-01-01 12:00");

                var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
                var initialAggregate = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1>());
                var expectedHistoryAfterFirstMigration = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1, E2>()).History;
                var expectedHistoryAfterSecondMigration = TestAggregate.FromEvents(container.Resolve<IUtcTimeTimeSource>(), id, Seq.OfTypes<Ec1, E1, E3, E2>()).History;

                Func<IEventStoreSession> session = () => container.Resolve<IEventStoreSession>();
                Func<IEventStore> eventStore = () => container.Resolve<IEventStore>();                

                container.ExecuteUnitOfWorkInIsolatedScope(() => session().Save(initialAggregate));
                migrations = firstMigration;
                var historyWithFirstMigrationUnPersisted = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);

                container.ExecuteUnitOfWorkInIsolatedScope(() => eventStore().PersistMigrations());
                var historyAfterPersistingFirstMigration = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyWithFirstMigrationUnPersisted, nameof(historyWithFirstMigrationUnPersisted));
                AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyAfterPersistingFirstMigration, nameof(historyAfterPersistingFirstMigration));

                migrations = secondMigration;
                ClearEventstoreCache(container);
                var historyWithSecondMigrationUnPersisted = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);

                container.ExecuteUnitOfWorkInIsolatedScope(() => eventStore().PersistMigrations());
                var historyAfterPersistingSecondMigration = container.ExecuteUnitOfWorkInIsolatedScope(() => session().Get<TestAggregate>(id).History);
                AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyWithSecondMigrationUnPersisted, nameof(historyWithSecondMigrationUnPersisted));
                AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyAfterPersistingSecondMigration, nameof(historyAfterPersistingSecondMigration));
            }
        }

        protected void ClearEventstoreCache(WindsorContainer container)
        {
            if(EventStoreType == typeof(SqlServerEventStore))
            {
                container.ExecuteUnitOfWorkInIsolatedScope(() => ((SqlServerEventStore)container.Resolve<IEventStore>()).ClearCache());
            }
        }

        [Test]
        public void Inserting_E2_Before_E1()
        {
            RunMigrationTest(
                new MigrationScenario(
                    Seq.OfTypes<Ec1, E1>(),
                    Seq.OfTypes<Ec1, E2, E1>(),
                    Before<E1>.Insert<E2>()));
        }
    }

    [TestFixture]
    public class DropTempdatabases
    {
        [Test]
        public void DropEm()
        {
            //new TemporaryLocalDbManager(new ConnectionStringConfigurationParameterProvider().GetConnectionString("MasterDB").ConnectionString)
            //    .RemoveAllDatabases();
        }
    }
}
