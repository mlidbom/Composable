using Composable.CQRS.KeyValueStorage;
using Composable.Persistence.KeyValueStorage;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    class InMemoryDocumentDbTests : DocumentDbTests
    {
        protected override IDocumentDb CreateStore() => new InMemoryDocumentDb();
    }
}