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
        internal ComposableDependencyInjectionContainer(IRunMode runMode) => RunMode = runMode;

        public IRunMode RunMode { get; }

        public void Register(params ComponentRegistration[] registrations)
        {
            Assert.State.Assert(!_createdServiceLocator);

            registrations.ForEach(registration => _registeredComponents.Add(registration.Id, registration));
            foreach(var registration in registrations)
            {
                if(registration.Lifestyle == Lifestyle.Singleton)
                {
                    _singletons.Add(registration);
                }
                foreach(var registrationServiceType in registration.ServiceTypes)
                {
                    _serviceToRegistrationDictionary.GetOrAdd(registrationServiceType, () => new List<ComponentRegistration>()).Add(registration);
                }
            }
        }

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents.Values.ToList();

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
        {
            _createdServiceLocator = true;
            return this;
        }

        bool _createdServiceLocator;

        TService IServiceLocator.Resolve<TService>() => Resolve<TService>();
        TService IServiceLocatorKernel.Resolve<TService>() => Resolve<TService>();
        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>() => new ComponentLease<TComponent>(Resolve<TComponent>());
        IMultiComponentLease<TComponent> IServiceLocator.LeaseAll<TComponent>() => throw new NotImplementedException();


        IDisposable IServiceLocator.BeginScope()
        {
            if(_scopedOverlay.Value?.IsDisposed == false)
            {
                throw new Exception("Someone failed to dispose a scope.");
            }

            _scopedOverlay.Value = new ComponentLifestyleOverlay(this);

            return Disposable.Create(EndScope);
        }

        void EndScope() => _scopedOverlay.Value.Dispose();

        TService Resolve<TService>()
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

            registration = registrations[0];

            switch(registration.Lifestyle)
            {
                case Lifestyle.Singleton:
                    return (TService)registration.GetSingletonInstance(this);
                case Lifestyle.Scoped:
                    return (TService)_scopedOverlay.Value.ResolveInstance(registration);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        readonly List<ComponentRegistration> _singletons = new List<ComponentRegistration>();
        readonly AsyncLocal<ComponentLifestyleOverlay> _scopedOverlay = new AsyncLocal<ComponentLifestyleOverlay>();
        readonly Dictionary<Guid, ComponentRegistration> _registeredComponents = new Dictionary<Guid, ComponentRegistration>();
        readonly IDictionary<Type, List<ComponentRegistration>> _serviceToRegistrationDictionary = new Dictionary<Type, List<ComponentRegistration>>();


        bool _disposed;
        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                foreach(var singleton in _singletons)
                {
                    singleton.Dispose();
                }
            }
        }

        class ComponentLifestyleOverlay
        {
            readonly ComposableDependencyInjectionContainer _parent;
            public ComponentLifestyleOverlay(ComposableDependencyInjectionContainer parent) => _parent = parent;
            readonly List<IDisposable> _disposables = new List<IDisposable>();
            readonly Dictionary<Guid, object> _instantiatedComponents = new Dictionary<Guid, object>();
            internal bool IsDisposed { get; private set; }
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

            public object ResolveInstance(ComponentRegistration registration)
            {
                Assert.State.Assert(!IsDisposed);
                if(_instantiatedComponents.TryGetValue(registration.Id, out var cachedInstance))
                {
                    return cachedInstance;
                } else
                {
                    cachedInstance = registration.CreateInstance(_parent);
                    _instantiatedComponents.Add(registration.Id, cachedInstance);
                    if(cachedInstance is IDisposable disposable)
                    {
                        _disposables.Add(disposable);
                    }
                    return cachedInstance;
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
