namespace Composable.KeyValueStorage
{
    public interface IKeyValueStore
    {
        IKeyValueSession OpenSession();
    }
}