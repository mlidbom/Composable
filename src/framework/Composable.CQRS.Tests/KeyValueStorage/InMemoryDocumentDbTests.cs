using Composable.DependencyInjection;
using NUnit.Framework;

namespace Composable.Tests.KeyValueStorage
{
    [TestFixture]
    class InMemoryDocumentDbTests : DocumentDbTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.InMemory);
    }
}