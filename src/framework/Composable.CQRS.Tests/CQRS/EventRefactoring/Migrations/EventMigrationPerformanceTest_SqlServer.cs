using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    [TestFixture]
    public class EventMigrationPerformanceTest_SqlServer : EventMigrationPerformanceTest
    {
        public EventMigrationPerformanceTest_SqlServer() : base(typeof(EventStore)) {}
    }
}
