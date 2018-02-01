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
    //Todo: put all components in a list and assign the index in the list to the component registration.
    //Then create matching arrays. One to cache all singletons. One no cache each scoped component in the scope overlay.
    //Use the static cache trick to give each component a unique index that never changes during runtime. It does not matter that this might lead to "holes" in the cache arrays.
    partial class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        bool _createdServiceLocator;
        readonly AsyncLocal<Scope> _scope = new AsyncLocal<Scope>();
        readonly List<ComponentRegistration> _singletons = new List<ComponentRegistration>();
        readonly Dictionary<Guid, ComponentRegistration> _registeredComponents = new Dictionary<Guid, ComponentRegistration>();
        readonly IDictionary<Type, List<ComponentRegistration>> _serviceToRegistrationDictionary = new Dictionary<Type, List<ComponentRegistration>>();
        readonly IDisposable _scopeDisposer;

        ComponentCache _cache;

        int _maxComponentIndex;

        public IRunMode RunMode { get; }

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents.Values.ToList();

        internal ComposableDependencyInjectionContainer(IRunMode runMode)
        {
            _scopeDisposer = Disposable.Create(EndScope);
            RunMode = runMode;
        }

        public void Register(params ComponentRegistration[] registrations)
        {
            Assert.State.Assert(!_createdServiceLocator);

            foreach(var registration in registrations)
            {
                _maxComponentIndex = Math.Max(_maxComponentIndex, registration.ComponentIndex);
                _registeredComponents.Add(registration.Id, registration);

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

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
        {
            if(!_createdServiceLocator)
            {
                _cache = new ComponentCache(_registeredComponents.Values);//Don't create in the constructor because not all registrations are done and thus new component indexes might appear thus breaking the cache.
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

        TService IServiceLocator.Resolve<TService>() => Resolve<TService>();
        TService[] IServiceLocator.ResolveAll<TService>() => throw new NotImplementedException();
        TService IServiceLocatorKernel.Resolve<TService>() => Resolve<TService>();

        IDisposable IServiceLocator.BeginScope()
        {
            if(_scope.Value?.IsDisposed == false)
            {
                throw new Exception("Someone failed to dispose a scope.");
            }

            _scope.Value = new Scope();

            return _scopeDisposer;
        }

        void EndScope() => _scope.Value.Dispose();

        TService Resolve<TService>()
        {
            if(_resolvingComponent != null && _resolvingComponent.TryResolveDependency<TService>(out var service))
            {
                return service;
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
                        if(previousResolvingComponent != null)
                        {
                            return previousResolvingComponent.ResolveSingletonDependency(serviceType, registration, this);
                        } else
                        {
                            return registration.GetSingletonInstance(this);
                        }
                    }
                    case Lifestyle.Scoped:
                        return _scope.Value.ResolveInstance(registration, this);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                _resolvingComponent = previousResolvingComponent;
            }
        }

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
    }
}
