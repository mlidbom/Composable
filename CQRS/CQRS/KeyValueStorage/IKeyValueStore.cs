namespace Composable.KeyValueStorage
{
    public interface IKeyValueStore
    {
        IKeyValueStoreSession OpenSession();
    }
}