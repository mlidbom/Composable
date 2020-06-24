using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventRefactoring.Migrations
{
    //urgent: Merge into base class
    [TestFixture]
    public class EventMigrationPerformanceTest_SqlServer : EventMigrationPerformanceTest
    {
        public EventMigrationPerformanceTest_SqlServer() : base(typeof(EventStore)) {}
    }
}
