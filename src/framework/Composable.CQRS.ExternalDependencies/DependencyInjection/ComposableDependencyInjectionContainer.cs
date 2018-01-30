using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading.ResourceAccess;

namespace Composable.DependencyInjection
{
    class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        internal ComposableDependencyInjectionContainer(IRunMode runMode)
        {
            RunMode = runMode;
            _state = new OptimizedThreadShared<NonThreadSafeImplementation>(new NonThreadSafeImplementation(this));
        }

        readonly IThreadShared<NonThreadSafeImplementation> _state;

        public IRunMode RunMode { get; }
        public void Register(params ComponentRegistration[] registrations) => _state.WithExclusiveAccess(state => state.Register(registrations));

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _state.WithExclusiveAccess(state => state.RegisteredComponents.Values.ToList());

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator() => _state.WithExclusiveAccess(state => state.CreateServiceLocator());

        TComponent IServiceLocator.Resolve<TComponent>() => ((IServiceLocatorKernel)this).Resolve<TComponent>();
        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>() => new ComponentLease<TComponent>(((IServiceLocatorKernel)this).Resolve<TComponent>());
        IMultiComponentLease<TComponent> IServiceLocator.LeaseAll<TComponent>() => new MultiComponentLease<TComponent>(_state.WithExclusiveAccess(state => state.ResolveAll<TComponent>()));
        IDisposable IServiceLocator.BeginScope() => _state.WithExclusiveAccess(state => state.BeginScope());

        TService IServiceLocatorKernel.Resolve<TService>()
        {
            ComponentRegistration registration = null;
            var overlay = _state.WithExclusiveAccess(state =>
            {
                if(!state.ServiceToRegistrationDictionary.TryGetValue(typeof(TService), out var registrations))
                {
                    throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
                }

                if(registrations.Count > 1)
                {
                    throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
                }

                registration = registrations.Single();
                switch(registration.Lifestyle)
                {
                    case Lifestyle.Singleton:
                        return state._singletonOverlay;
                    case Lifestyle.Scoped:
                        return state._scopedOverlay.Value;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            return (TService)overlay.WithExclusiveAccess(someOverlay =>
            {
                return someOverlay.ResolveInstance(registration);
            });
        }

        void IDisposable.Dispose() => _state.WithExclusiveAccess(state => state.Dispose());


        class NonThreadSafeImplementation
        {
            readonly ComposableDependencyInjectionContainer _parent;
            internal readonly Dictionary<Guid, ComponentRegistration> RegisteredComponents = new Dictionary<Guid, ComponentRegistration>();
            public readonly IDictionary<Type, List<ComponentRegistration>> ServiceToRegistrationDictionary = new Dictionary<Type, List<ComponentRegistration>>();

            internal readonly OptimizedThreadShared<ComponentLifestyleOverlay> _singletonOverlay;
            internal readonly AsyncLocal<OptimizedThreadShared<ComponentLifestyleOverlay>> _scopedOverlay = new AsyncLocal<OptimizedThreadShared<ComponentLifestyleOverlay>>();

            public NonThreadSafeImplementation(ComposableDependencyInjectionContainer parent)
            {
                _parent = parent;
                _singletonOverlay = new OptimizedThreadShared<ComponentLifestyleOverlay>(new ComponentLifestyleOverlay(_parent));
            }

            public void Register(ComponentRegistration[] registrations)
            {
                registrations.ForEach(registration => RegisteredComponents.Add(registration.Id, registration));
                foreach(var registration in registrations)
                {
                    foreach(var registrationServiceType in registration.ServiceTypes)
                    {
                        ServiceToRegistrationDictionary.GetOrAdd(registrationServiceType, () => new List<ComponentRegistration>()).Add(registration);
                    }
                }
            }

            public TService[] ResolveAll<TService>() where TService : class => throw new NotImplementedException();

            void Verify()
            {
                //todo: Implement some validation here?
            }

            bool _verified;

            internal IServiceLocator CreateServiceLocator()
            {
                if(!_verified)
                {
                    _verified = true;
                    Verify();
                }

                return _parent;
            }

            public IDisposable BeginScope()
            {
                if(_scopedOverlay.Value != null)
                {
                    throw new Exception("Already has scope....");
                }

                _scopedOverlay.Value = new OptimizedThreadShared<ComponentLifestyleOverlay>(new ComponentLifestyleOverlay(_parent));

                return Disposable.Create(() =>
                {
                    var componentLifestyleOverlay =_parent._state.WithExclusiveAccess(state => state.EndScopeAndReturnScopedComponents());
                    componentLifestyleOverlay.WithExclusiveAccess(overlay => overlay.Dispose());
                });
            }

            OptimizedThreadShared<ComponentLifestyleOverlay> EndScopeAndReturnScopedComponents()
            {
                var scopeOverlay = _scopedOverlay.Value;
                _scopedOverlay.Value = null;
                return scopeOverlay;
            }

            bool _disposed;
            public void Dispose()
            {
                if(!_disposed)
                {
                    _disposed = true;
                    _singletonOverlay.WithExclusiveAccess(overlay => overlay.Dispose());
                }
            }
        }

        class ComponentLifestyleOverlay
        {
            readonly ComposableDependencyInjectionContainer _parent;
            public ComponentLifestyleOverlay(ComposableDependencyInjectionContainer parent) => _parent = parent;
            readonly Dictionary<Guid, CachedInstance> _instantiatedComponents = new Dictionary<Guid, CachedInstance>();
            bool _disposed;
            public void Dispose()
            {
                if(!_disposed)
                {
                    _disposed = true;
                    _instantiatedComponents
                        .ToList()
                       .Where(cached => !cached.Value.CreationSpecIsInstance)
                        .Select(singleton => singleton.Value.Instance)
                        .OfType<IDisposable>()
                        .ForEach(disposable => disposable.Dispose());
                }
            }

            public object ResolveInstance(ComponentRegistration registration)
            {
                if(_instantiatedComponents.TryGetValue(registration.Id, out var cachedInstance))
                {
                    return cachedInstance.Instance;
                } else
                {
                    cachedInstance = CreateRegistrationInstance(registration);
                    _instantiatedComponents.Add(registration.Id, cachedInstance);
                    return cachedInstance.Instance;
                }
            }

            CachedInstance CreateRegistrationInstance(ComponentRegistration registration)
            {
                if(registration.InstantiationSpec.FactoryMethod != null)
                {
                    return new CachedInstance(creationSpecIsInstance: false, instance: registration.InstantiationSpec.FactoryMethod(_parent));
                } else if(registration.InstantiationSpec.Instance is object instance)
                {
                    return new CachedInstance(creationSpecIsInstance: true, instance: instance);
                }else
                {
                    throw new Exception("Failed to create instance");
                }
            }

            class CachedInstance
            {
                public CachedInstance(bool creationSpecIsInstance, object instance)
                {
                    CreationSpecIsInstance = creationSpecIsInstance;
                    Instance = instance;
                }

                internal bool CreationSpecIsInstance{get;}
                internal object Instance { get; }
            }
        }
    }

    sealed class ComponentLease<T> : IComponentLease<T>
    {
        readonly T _instance;

        internal ComponentLease(T component) => _instance = component;

        T IComponentLease<T>.Instance => _instance;
        void IDisposable.Dispose() {}
    }

    sealed class MultiComponentLease<T> : IMultiComponentLease<T>
    {
        readonly T[] _instances;

        internal MultiComponentLease(T[] components) => _instances = components;

        T[] IMultiComponentLease<T>.Instances => _instances;
        void IDisposable.Dispose() {}
    }
}
