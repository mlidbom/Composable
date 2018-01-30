using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TestFixture]
    public class EventMigrationPerformanceTest_InMemory : EventMigrationPerformanceTest
    {
        public EventMigrationPerformanceTest_InMemory() : base(typeof(InMemoryEventStore)) {}
    }
}
