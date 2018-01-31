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
    //todo: Cache singletons (and other registrations) with the components that need them.
    class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        readonly IDisposable _scopeDisposer;
        internal ComposableDependencyInjectionContainer(IRunMode runMode)
        {
            _scopeDisposer = Disposable.Create(EndScope);
            RunMode = runMode;
        }

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
            if(!_createdServiceLocator)
            {
                _createdServiceLocator = true;
                Verify();
            }

            return this;
        }

        void Verify()
        {
            using(((IServiceLocator)this).BeginScope())
            {
                var allServiceTypes = _serviceToRegistrationDictionary.Values
                                                                      .SelectMany(registrations => registrations)
                                                                      .SelectMany(registration => registration.ServiceTypes)
                                                                      .ToList();

                foreach(var serviceType in allServiceTypes)
                {
                    Resolve(serviceType);
                }
            }
        }

        bool _createdServiceLocator;

        TService IServiceLocator.Resolve<TService>() => Resolve<TService>();
        TService[] IServiceLocator.ResolveAll<TService>() => throw new NotImplementedException();
        TService IServiceLocatorKernel.Resolve<TService>() => Resolve<TService>();

        IDisposable IServiceLocator.BeginScope()
        {
            if(_scopedOverlay.Value?.IsDisposed == false)
            {
                throw new Exception("Someone failed to dispose a scope.");
            }

            _scopedOverlay.Value = new ScopedComponentOverlay();

            return _scopeDisposer;
        }

        void EndScope() => _scopedOverlay.Value.Dispose();

        TService Resolve<TService>()
        {
            if(_resolvingComponent != null)
            {
                foreach(var cachedSingletonDependency in _resolvingComponent?.CachedSingletonDependencies)
                {
                    if(cachedSingletonDependency is TService service)
                    {
                        return service;
                    }
                }
            }

            return (TService)Resolve(typeof(TService));
        }

        [ThreadStatic] static ComponentRegistration _resolvingComponent;
        object Resolve(Type serviceType)
        {
            if(!_serviceToRegistrationDictionary.TryGetValue(serviceType, out var registrations))
            {
                throw new Exception($"No service of type: {serviceType.GetFullNameCompilable()} is registered.");
            }

            if(registrations.Count > 1)
            {
                throw new Exception($"Requested single instance for service:{serviceType}, but there were multiple services registered.");
            }

            var registration = registrations[0];

            if(_resolvingComponent?.Lifestyle == Lifestyle.Singleton && registration.Lifestyle != Lifestyle.Singleton)
            {
                throw new Exception($"{Lifestyle.Singleton} service: {_resolvingComponent.ServiceTypes.First().FullName} depends on {registration.Lifestyle} service: {registration.ServiceTypes.First().FullName} ");
            }

            var previousResolvingComponent = _resolvingComponent;
            _resolvingComponent = registration;
            try
            {
                switch(registration.Lifestyle)
                {
                    case Lifestyle.Singleton:
                    {
                        var result = registration.GetSingletonInstance(this);
                        previousResolvingComponent?.CachedSingletonDependencies.Add(result);
                        return result;
                    }
                    case Lifestyle.Scoped:
                        return _scopedOverlay.Value.ResolveInstance(registration, this);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                _resolvingComponent = previousResolvingComponent;
            }
        }

        readonly List<ComponentRegistration> _singletons = new List<ComponentRegistration>();
        readonly AsyncLocal<ScopedComponentOverlay> _scopedOverlay = new AsyncLocal<ScopedComponentOverlay>();
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

        class ScopedComponentOverlay
        {
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

            public object ResolveInstance(ComponentRegistration registration, IServiceLocatorKernel parent)
            {
                if(_instantiatedComponents.TryGetValue(registration.Id, out var cachedInstance))
                {
                    return cachedInstance;
                } else
                {
                    cachedInstance = registration.CreateInstance(parent);
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
}
