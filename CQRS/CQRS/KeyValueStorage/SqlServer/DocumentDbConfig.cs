using System;

namespace Composable.KeyValueStorage.SqlServer
{
    public class DocumentDbConfig
    {
        public static readonly DocumentDbConfig Default = new DocumentDbConfig(NullOpDocumentDbSessionInterceptor.Instance);

        public DocumentDbConfig(IDocumentDbSessionInterceptor interceptor = null)
        {
            Interceptor = interceptor ?? NullOpDocumentDbSessionInterceptor.Instance;
        }

        public IDocumentDbSessionInterceptor Interceptor { get; private set; }

    }
}