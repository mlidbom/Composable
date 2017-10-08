using Composable.DependencyInjection;
using NUnit.Framework;

namespace Composable.Tests.CQRS
{
    [TestFixture]
    class InMemoryEventStoreSessionTests : EventStoreSessionTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.InMemory);
    }
}