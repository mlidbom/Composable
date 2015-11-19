using System;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.System.Linq;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;
using TestAggregates.Events;


namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class InMemoryEventStoreEventStreamMutatorTests : EventStreamMutatorTests
    {
        public InMemoryEventStoreEventStreamMutatorTests() : base(typeof(InMemoryEventStore)){}
    }

    public class SqlServerEventStoreEventStreamMutatorTests : EventStreamMutatorTests
    {
        public SqlServerEventStoreEventStreamMutatorTests() : base(typeof(SqlServerEventStore)) { }
        [SetUp]
        public void SetupTask()
        {
            SqlServerEventStore.ResetDB(ConnectionString);
        }
    }

    [TestFixture, ExclusivelyUses(NCrunchExlusivelyUsesResources.EventStoreDbMdf)]
    public abstract class EventStreamMutatorTests : EventStreamMutatorTestsBase
    {
        protected EventStreamMutatorTests(Type eventStoreType) : base(eventStoreType) {}


        [Test]
        public void Replacing_E1_with_E2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E2, Ef, Ef>(),
                Replace<E1>.With<E2>());
        }

        [Test]
        public void Replacing_E1_with_E2_at_end_of_stream()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2>(),
                Replace<E1>.With<E2>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef>(),
                Replace<E1>.With<E2, E3>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef, Ef, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef, Ef, Ef, Ef>(),
                Replace<E1>.With<E2, E3>());
        }

        [Test]
        public void Replacing_E1_with_E2_then_irrelevant_migration()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, Ef>(),
                Replace<E1>.With<E2>(),
                Replace<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E2, E3, Ef>(),
                Replace<E1>.With<E2, E3>(),
                Replace<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_E2_with_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E4, E3, Ef>(),
                Replace<E1>.With<E2,E3>(),
                Replace<E2>.With<E4>());
        }


        [Test]
        public void Inserting_E3_before_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E1, Ef>(),
                Before<E1>.Insert<E3>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E4, E1, Ef>(),
                Before<E1>.Insert<E3, E4>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E5, E3, E4, E1>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E3>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E4, E1, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E6, E1, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>(),
                Replace<E3>.With<E6>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6_then_replace_Ef_with_E7_then_insert_E8_after_E7()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E6, E1, E7, E8>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>(),
                Replace<E3>.With<E6>(),
                Replace<Ef>.With<E7>(),
                After<E7>.Insert<E8>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E3_2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E5, E3, E4, E1, Ef, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E3>.Insert<E5>());
        }

        [Test]
        public void Inserting_E3_E4_before_E1_then_E5_before_E4_2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E3, E5, E4, E1, Ef, Ef>(),
                Before<E1>.Insert<E3, E4>(),
                Before<E4>.Insert<E5>());
        }

        [Test]
        public void Inserting_E2_after_E1()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1, Ef, Ef>(),
                Seq.OfTypes<Ec1, E1, E2, Ef, Ef>(),
                After<E1>.Insert<E2>());
        }

        [Test]
        public void Inserting_E2_after_E1_at_end_of_stream()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E1, E2>(),
                After<E1>.Insert<E2>());
        }


        [Test]
        public void Given_Ec1_E1_before_E1_E2_after_E2_E3_throws_NonIdempotentMigrationDetectedException()
        {
            this.Invoking( me => 
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2, E3, E1>(),
                Before<E1>.Insert<E2>(),
                After<E2>.Insert<E3>()))
                .ShouldThrow<NonIdempotentMigrationDetectedException>();
        }
    }
}