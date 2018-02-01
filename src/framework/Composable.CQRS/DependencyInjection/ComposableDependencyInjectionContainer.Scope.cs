namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        class Scope
        {
            internal readonly ComponentCache Cache;
            internal bool IsDisposed { get; private set; }

            public Scope(ComposableDependencyInjectionContainer container) => Cache = container._singletonCache.Clone();

            public void Dispose()
            {
                if(!IsDisposed)
                {
                    IsDisposed = true;
                    Cache.Dispose();
                }
            }

            public object ResolveInstance(ComponentRegistration registration, IServiceLocatorKernel parent)
            {
                var newInstance = registration.CreateInstance(parent);
                Cache.Set(newInstance, registration);
                return newInstance;
            }
        }
    }
}
