using Composable.Persistence.EventStore;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations {
    public class EventMigrationPerformanceTest_SqlServer : EventMigrationPerformanceTest
    {
        public EventMigrationPerformanceTest_SqlServer() : base(typeof(EventStore)) { }
    }
}