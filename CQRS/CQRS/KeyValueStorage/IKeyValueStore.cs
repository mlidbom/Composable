using Composable.KeyValueStorage.SqlServer;

namespace Composable.KeyValueStorage
{
    public interface IKeyValueStore
    {
        IObjectStore CreateStore();
        IKeyValueStoreSession OpenSession();
    }
}