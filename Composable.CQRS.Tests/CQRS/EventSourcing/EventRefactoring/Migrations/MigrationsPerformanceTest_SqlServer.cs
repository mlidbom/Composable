using Composable.Persistence.EventStore;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations {
    public class MigrationsPerformanceTest_SqlServer : MigrationsPerformanceTest
    {
        public MigrationsPerformanceTest_SqlServer() : base(typeof(EventStore)) { }
    }
}