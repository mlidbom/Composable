using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Testing;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    class InMemoryEventStoreTests : EventStoreTests
    {
        protected override IEventStore CreateStore()
        {
            return new InMemoryEventStore(new DummyServiceBus(new WindsorContainer()));
        }
    }
}