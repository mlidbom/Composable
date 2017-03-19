using Composable.DocumentDb;
using Composable.Persistence.DocumentDb;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    class InMemoryDocumentDbTests : DocumentDbTests
    {
        protected override IDocumentDb CreateStore() => new InMemoryDocumentDb();
    }
}