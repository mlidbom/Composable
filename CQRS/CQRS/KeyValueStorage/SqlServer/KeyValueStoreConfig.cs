using System;

namespace Composable.KeyValueStorage.SqlServer
{
    public class KeyValueStoreConfig
    {
        public static readonly KeyValueStoreConfig Default = new KeyValueStoreConfig();

        private IKeyValueStoreInterceptor _interceptor;
        private Func<IKeyValueStoreInterceptor> _interceptorFactory;        

        public Func<IKeyValueStoreInterceptor> InterceptorFactory
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

        public IKeyValueStoreInterceptor Interceptor
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
                return NullOpKeyValueStoreInterceptor.Instance;
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