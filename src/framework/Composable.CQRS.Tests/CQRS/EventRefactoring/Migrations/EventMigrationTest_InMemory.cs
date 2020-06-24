using Composable.Persistence.EventStore;
using Composable.Persistence.InMemory.EventStore;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TestFixture] public class EventMigrationTest_InMemory : EventMigrationTest
    {
        public EventMigrationTest_InMemory() : base(typeof(InMemoryEventStore)) {}
    }
}
