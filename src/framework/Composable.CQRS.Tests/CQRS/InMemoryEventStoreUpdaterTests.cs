using Composable.DependencyInjection;
using NUnit.Framework;

namespace Composable.Tests.CQRS
{
    [TestFixture]
    class InMemoryEventStoreUpdaterTests : EventStoreUpdaterTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.InMemory);
    }
}