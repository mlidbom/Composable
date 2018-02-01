using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.Contracts;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Reflection;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        bool _createdServiceLocator;
        readonly AsyncLocal<ComponentCache> _scopeCache = new AsyncLocal<ComponentCache>();
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
                _cache = new ComponentCache(_registeredComponents.Values.ToList());//Don't create in the constructor because no registrations are done and thus new component indexes will appear, thus breaking the cache.
                _createdServiceLocator = true;
                Verify();
            }

            return this;
        }

        void Verify()
        {
            using(((IServiceLocator)this).BeginScope())
            {
                foreach(var component in _registeredComponents.Values)
                {
                    component.Resolve(this);
                }
            }
        }

        TService IServiceLocator.Resolve<TService>() => Resolve<TService>();
        TService[] IServiceLocator.ResolveAll<TService>() => throw new NotImplementedException();
        TService IServiceLocatorKernel.Resolve<TService>() => Resolve<TService>();

        IDisposable IServiceLocator.BeginScope()
        {
            if(_scopeCache.Value?.IsDisposed == false)
            {
                throw new Exception("Someone failed to dispose a scope.");
            }

            _scopeCache.Value = _cache.Clone();

            return _scopeDisposer;
        }

        void EndScope() => _scopeCache.Value.Dispose();

        [ThreadStatic] static ComponentRegistration _resolvingComponent;
        TService Resolve<TService>()
        {
            if(_cache.TryGet<TService>() is TService singleton)
            {
                return singleton;
            }

            var scopeCache = _scopeCache.Value;

            if (scopeCache.TryGet<TService>() is TService scoped)
            {
                return scoped;
            }

            var registrations = _cache.GetRegistration<TService>();

            if (registrations == null)
            {
                throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
            }

            if(registrations.Length > 1)
            {
                throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
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
                        var createdSingleton = registration.GetSingletonInstance(this);
                        _cache.Set(createdSingleton, registration);
                        return (TService)createdSingleton;
                    }
                    case Lifestyle.Scoped:
                    {
                        var newInstance = registration.CreateInstance(this);
                        scopeCache.Set(newInstance, registration);
                        return (TService)newInstance;
                    }
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
