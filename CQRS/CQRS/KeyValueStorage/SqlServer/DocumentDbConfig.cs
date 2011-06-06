using System;

namespace Composable.KeyValueStorage.SqlServer
{
    public class DocumentDbConfig
    {
        public static readonly DocumentDbConfig Default = new DocumentDbConfig();

        private IDocumentDbSessionInterceptor _interceptor;
        private Func<IDocumentDbSessionInterceptor> _interceptorFactory;        

        public Func<IDocumentDbSessionInterceptor> InterceptorFactory
        {
            get { return _interceptorFactory; }
            set
            {
                if (InterceptorFactory != null)
                {
                    throw new CannotSetBothInterceptorAndFactoryMethodForInterceptor();
                }
                _interceptorFactory = value;
            }
        }

        public IDocumentDbSessionInterceptor Interceptor
        {
            get
            {
                if (_interceptor != null)
                {
                    return _interceptor;
                }
                if (InterceptorFactory != null)
                {
                    return InterceptorFactory();
                }
                return NullOpDocumentDbSessionInterceptor.Instance;
            }
            set
            {
                if (InterceptorFactory != null)
                {
                    throw new CannotSetBothInterceptorAndFactoryMethodForInterceptor();
                }
                _interceptor = value;
            }
        }
    }
}