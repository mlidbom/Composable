using Composable.KeyValueStorage;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    class InMemoryKeyValueStoreTests : KeyValueStoreTests
    {
        protected override IKeyValueStore CreateStore()
        {
            return new InMemoryKeyValueStore();
        }
    }
}