using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;

namespace Composable.KeyValueStorage
{
    public class InMemoryKeyValueStore : IKeyValueStore
    {
        private readonly IObjectStore _store = new InMemoryObjectStore();
        protected InMemoryKeyValueStoreConfig Config { get; private set; }

        public IObjectStore CreateStore()
        {
            return _store;
        }

        public InMemoryKeyValueStore(InMemoryKeyValueStoreConfig config = null)
        {
            if (config == null)
            {
                Config = InMemoryKeyValueStoreConfig.Default;
            }
            else
            {
                Config = config;
            }
        }

        public IKeyValueStoreSession OpenSession()
        {
            return new KeyValueSession(this, this.Config);
        }
    }
}