using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TestFixture, Ignore("todo: Funnily the in-memory event store is to slow for the cached tests :)")]
    public class EventMigrationPerformanceTest_InMemory : EventMigrationPerformanceTest
    {
        public EventMigrationPerformanceTest_InMemory() : base(typeof(InMemoryEventStore)) {}
    }
}
