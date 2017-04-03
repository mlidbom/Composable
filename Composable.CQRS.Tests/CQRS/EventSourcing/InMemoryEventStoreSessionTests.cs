using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    class InMemoryEventStoreSessionTests : EventStoreSessionTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.InMemory);
        protected override IEventStore CreateStore() => new InMemoryEventStore();
    }
}