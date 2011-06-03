using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueStore : IKeyValueStore
    {
        internal readonly InMemoryObjectStore Db = new InMemoryObjectStore();

        public IKeyValueStoreSession OpenSession()
        {
            return new InMemoryKeyValueSession(this);
        }
    }
}