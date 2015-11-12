using Composable.System.Linq;
using NUnit.Framework;
using TestAggregates.Events;


namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture]
    public class EventStreamMutatorTests : EventStreamMutatorTestsBase
    {
        [Test]
        public void Replacing_one_event_with_one_event()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2>(),
                ReplaceEventType<E1>.With<E2>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2, E3>(),
                ReplaceEventType<E1>.With<E2, E3>());
        }

        [Test]
        public void Replacing_E1_with_E2_then_irrelevant_migration()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2>(),
                ReplaceEventType<E1>.With<E2>(),
                ReplaceEventType<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E2, E3>(),
                ReplaceEventType<E1>.With<E2, E3>(),
                ReplaceEventType<E1>.With<E5>());
        }

        [Test]
        public void Replacing_E1_with_E2_E3_then_E2_with_E4()
        {
            RunMigrationTest(
                Seq.OfTypes<Ec1, E1>(),
                Seq.OfTypes<Ec1, E4, E3>(),
                ReplaceEventType<E1>.With<E2,E3>(),
                ReplaceEventType<E2>.With<E4>());
        }
    }
}