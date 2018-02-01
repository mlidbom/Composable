using System;
using System.Collections.Generic;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        class Scope
        {
            readonly List<IDisposable> _disposables = new List<IDisposable>();
            readonly Dictionary<Guid, object> _instantiatedComponents = new Dictionary<Guid, object>();
            internal readonly ComponentCache Cache;
            internal bool IsDisposed { get; private set; }

            public Scope(ComposableDependencyInjectionContainer container) => Cache = container._singletonCache.Clone();

            public void Dispose()
            {
                if(!IsDisposed)
                {
                    IsDisposed = true;
                    foreach(var disposable in _disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }

            public object ResolveInstance(ComponentRegistration registration, IServiceLocatorKernel parent)
            {
                var newInstance = registration.CreateInstance(parent);
                Cache.Set(newInstance, registration);
                _instantiatedComponents.Add(registration.Id, newInstance);
                if(newInstance is IDisposable disposable)
                {
                    _disposables.Add(disposable);
                }

                return newInstance;
            }
        }
    }
}
