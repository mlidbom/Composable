using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.DependencyInjection
{
    class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        internal ComposableDependencyInjectionContainer(IRunMode runMode)
        {
            RunMode = runMode;
            _singletonOverlay = new ComponentLifestyleOverlay(this);
        }

        public IRunMode RunMode { get; }

        public void Register(params ComponentRegistration[] registrations)
        {
            Assert.State.Assert(!_createdServiceLocator);
            registrations.ForEach(registration => _registeredComponents.Add(registration.Id, registration));
            foreach(var registration in registrations)
            {
                foreach(var registrationServiceType in registration.ServiceTypes)
                {
                    _serviceToRegistrationDictionary.GetOrAdd(registrationServiceType, () => new List<ComponentRegistration>()).Add(registration);
                }
            }
        }

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents.Values.ToList();

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator() => Locked(() =>
        {

            if(!_createdServiceLocator)
            {
                _createdServiceLocator = true;
            }

            return this;
        });

        bool _createdServiceLocator;

        TService IServiceLocator.Resolve<TService>() => Resolve<TService>();
        TService IServiceLocatorKernel.Resolve<TService>() => Resolve<TService>();
        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>() => new ComponentLease<TComponent>(Resolve<TComponent>());
        IMultiComponentLease<TComponent> IServiceLocator.LeaseAll<TComponent>() => throw new NotImplementedException();


        IDisposable IServiceLocator.BeginScope() => Locked(() =>
        {
            if(_scopedOverlay.Value != null)
            {
                throw new Exception("Already has scope....");
            }

            _scopedOverlay.Value = new ComponentLifestyleOverlay(this);

            return Disposable.Create(EndScope);
        });

        void EndScope()
        {
            var overlay = Locked(() =>
            {
                var scopeOverlay = _scopedOverlay.Value;
                _scopedOverlay.Value = null;
                return scopeOverlay;
            });

            overlay.Dispose();
        }

        TService Resolve<TService>() => Locked(() =>
        {
            ComponentRegistration registration = null;
            if(!_serviceToRegistrationDictionary.TryGetValue(typeof(TService), out var registrations))
            {
                throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
            }

            if(registrations.Count > 1)
            {
                throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
            }

            registration = registrations.Single();
            ComponentLifestyleOverlay overlay;

            switch(registration.Lifestyle)
            {
                case Lifestyle.Singleton:
                    overlay = _singletonOverlay;
                    break;
                case Lifestyle.Scoped:
                    overlay = _scopedOverlay.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (TService)overlay.ResolveInstance(registration);
        });

        readonly ComponentLifestyleOverlay _singletonOverlay;
        readonly AsyncLocal<ComponentLifestyleOverlay> _scopedOverlay = new AsyncLocal<ComponentLifestyleOverlay>();
        readonly Dictionary<Guid, ComponentRegistration> _registeredComponents = new Dictionary<Guid, ComponentRegistration>();
        readonly IDictionary<Type, List<ComponentRegistration>> _serviceToRegistrationDictionary = new Dictionary<Type, List<ComponentRegistration>>();


        bool _disposed;
        public void Dispose() => Locked(() =>
        {
            if(!_disposed)
            {
                _disposed = true;
                _singletonOverlay.Dispose();
            }
        });

        readonly object _lock = new object();
        bool _uselock = true;
        TResult Locked<TResult>(Func<TResult> locked)
        {
            if(_uselock)
            {
                lock(_lock)
                {
                    return locked();
                }
            } else
            {
                return locked();
            }
        }

        void Locked(Action locked)
        {
            if(_uselock)
            {
                lock(_lock)
                {
                    locked();
                }
            } else
            {
                locked();
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
                    _instantiatedComponents.ForEach(cached => cached.Value.Dispose());
                }
            }

            public object ResolveInstance(ComponentRegistration registration)
            {
                Assert.State.Assert(!_disposed);
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

            class CachedInstance : IDisposable
            {
                public CachedInstance(bool creationSpecIsInstance, object instance)
                {
                    CreationSpecIsInstance = creationSpecIsInstance;
                    Instance = instance;
                }

                bool CreationSpecIsInstance{get;}
                internal object Instance { get; }

                bool _disposed;
                public void Dispose()
                {
                    if(!_disposed)
                    {
                        _disposed = true;
                        if(!CreationSpecIsInstance)
                        {
                            if(Instance is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                }
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
