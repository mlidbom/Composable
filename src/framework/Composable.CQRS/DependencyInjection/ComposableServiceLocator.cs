using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.System;
using Composable.System.Reflection;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        class ComposableServiceLocator : IServiceLocator, IServiceLocatorKernel
        {
            readonly AsyncLocal<ScopeCache?> _scopeCache = new AsyncLocal<ScopeCache?>();
            readonly RootCache _rootCache;

            bool _disposed;
            public ComposableServiceLocator(List<ComponentRegistration> components) => _rootCache = new RootCache(components);

            TService[] IServiceLocator.ResolveAll<TService>() => throw new NotImplementedException();

            IDisposable IServiceLocator.BeginScope()
            {
                Assert.State.Assert(!_disposed);
                if(_scopeCache.Value != null)
                {
                    throw new Exception("Scope already exists. Nested scopes are not supported.");
                }

                _scopeCache.Value = _rootCache!.CreateScopeCache();

                return Disposable.Create(EndScope);
            }

            void EndScope()
            {
                var scopeCacheValue = _scopeCache.Value;
                if(scopeCacheValue == null)
                {
                    throw new Exception("Attempt to dispose scope from a context that is not within the scope.");
                }

                scopeCacheValue.Dispose();
                _scopeCache.Value = null;
            }

            [ThreadStatic] static ComponentRegistration? _parentComponent;
            public TService Resolve<TService>() where TService : class
            {
                Assert.State.Assert(!_disposed);
                if(TryGetFromCache(out var registrations, out var currentComponent, out TService? cached)) return cached;

                if(_parentComponent?.Lifestyle == Lifestyle.Singleton && currentComponent.Lifestyle != Lifestyle.Singleton)
                {
                    throw new Exception($"{Lifestyle.Singleton} service: {_parentComponent.ServiceTypes.First().FullName} depends on {currentComponent.Lifestyle} service: {currentComponent.ServiceTypes.First().FullName} ");
                }

                var previousResolvingComponent = _parentComponent;
                _parentComponent = currentComponent;

                try
                {
                    return CreateAndCacheInstance<TService>(currentComponent, _scopeCache.Value);
                }
                finally
                {
                    _parentComponent = previousResolvingComponent;
                }
            }

            bool TryGetFromCache<TService>(out ComponentRegistration[] registrations, out ComponentRegistration currentComponent, [NotNullWhen(true)] out TService? resolve) where TService : class
            {
                object cachedSingletonInstance;
                (registrations, cachedSingletonInstance) = _rootCache!.TryGet<TService>();
                currentComponent = Assert.Result.NotNull(registrations[0]);
                resolve = null;

                if(cachedSingletonInstance is TService singleton)
                {
                    resolve = singleton;
                    return true;
                }

                var scopeCache = _scopeCache.Value;

                // ReSharper disable once PatternAlwaysOfType Silly ReSharper is wrong again
                if(scopeCache != null && scopeCache.TryGet<TService>() is TService scoped)
                {
                    {
                        resolve = scoped;
                        return true;
                    }
                }

                if(registrations == null)
                {
                    throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
                }

                if(registrations.Length > 1)
                {
                    throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
                }

                return false;
            }

            TService CreateAndCacheInstance<TService>(ComponentRegistration currentComponent, ScopeCache? scopeCache) where TService : class
            {
                lock(currentComponent)
                {
                    TService instance;
                    switch(currentComponent.Lifestyle)
                    {
                        case Lifestyle.Singleton:
                        {
                            instance = (TService)currentComponent.InstantiationSpec.FactoryMethod(this);
                            _rootCache!.Set(instance, currentComponent);
                            return instance;
                        }
                        case Lifestyle.Scoped:
                        {
                            if(scopeCache == null) throw new Exception("Attempted to resolve scoped component without a scope");

                            instance = (TService)currentComponent.InstantiationSpec.FactoryMethod(this);
                            scopeCache.Set(instance, currentComponent);
                            return instance;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            public void Dispose()
            {
                if(!_disposed)
                {
                    _disposed = true;
                    _rootCache!.Dispose();
                }
            }
        }
    }
}