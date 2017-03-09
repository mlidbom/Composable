namespace Composable.KeyValueStorage
{
    public interface IDocumentDbSessionInterceptor
    {
        void AfterLoad(object instance);
    }

    public class NullOpDocumentDbSessionInterceptor : IDocumentDbSessionInterceptor
    {
        public static IDocumentDbSessionInterceptor Instance = new NullOpDocumentDbSessionInterceptor();

        NullOpDocumentDbSessionInterceptor()
        {

        }

        public void AfterLoad(object instance)
        {
        }
    }
}