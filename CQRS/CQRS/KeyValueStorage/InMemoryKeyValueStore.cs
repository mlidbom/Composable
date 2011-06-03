using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly IObjectStore _store = new InMemoryObjectStore();

        public IKeyValueStoreSession OpenSession()
        {
            return new KeyValueSession(_store);
        }
    }
}