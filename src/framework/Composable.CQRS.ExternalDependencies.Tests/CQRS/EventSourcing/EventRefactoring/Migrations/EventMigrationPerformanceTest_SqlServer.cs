using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [TestFixture]
    public class EventMigrationPerformanceTest_SqlServer : EventMigrationPerformanceTest
    {
        public EventMigrationPerformanceTest_SqlServer() : base(typeof(EventStore)) {}
    }
}
