using Composable.KeyValueStorage;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    class InMemoryDocumentDbTests : DocumentDbTests
    {
        protected override IDocumentDb CreateStore()
        {
            return new InMemoryDocumentDb();
        }        
    }
}