using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    [Ignore("todo: Funnily the in-memory event store is to slow for the cached tests :)")]
    public class MigrationsPerformanceTest_InMemory : MigrationsPerformanceTest
    {
        public MigrationsPerformanceTest_InMemory() : base(typeof(InMemoryEventStore)) {}
    }
}
