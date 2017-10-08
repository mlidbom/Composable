using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture] public class EventMigrationTest_InMemory : EventMigrationTest
    {
        public EventMigrationTest_InMemory() : base(typeof(InMemoryEventStore)) {}
    }
}
