namespace Composable.KeyValueStorage
{
    public interface IKeyValueStoreInterceptor
    {
        void AfterLoad(object instance);
    }
}