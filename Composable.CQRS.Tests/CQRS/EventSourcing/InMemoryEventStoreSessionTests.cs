using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    class InMemoryEventStoreSessionTests : EventStoreSessionTests
    {
        protected override IEventStore CreateStore() => new InMemoryEventStore();
    }
}