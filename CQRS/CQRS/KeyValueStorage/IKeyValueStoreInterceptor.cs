namespace Composable.KeyValueStorage
{
    public interface IKeyValueStoreInterceptor
    {
        void AfterLoad(object instance);
    }

    public class NullOpKeyValueStoreInterceptor : IKeyValueStoreInterceptor
    {
        public static IKeyValueStoreInterceptor Instance = new NullOpKeyValueStoreInterceptor();

        private NullOpKeyValueStoreInterceptor()
        {
            
        }

        public void AfterLoad(object instance)
        {
        }
    }
}