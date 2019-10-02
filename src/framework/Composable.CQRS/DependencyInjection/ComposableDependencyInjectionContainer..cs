﻿using System;
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
        readonly AsyncLocal<ScopeCache> _scopeCache = new AsyncLocal<ScopeCache>();
        readonly List<ComponentRegistration> _singletons = new List<ComponentRegistration>();
        readonly Dictionary<Guid, ComponentRegistration> _registeredComponents = new Dictionary<Guid, ComponentRegistration>();
        readonly IDictionary<Type, List<ComponentRegistration>> _serviceToRegistrationDictionary = new Dictionary<Type, List<ComponentRegistration>>();
#pragma warning disable IDE0069 // Disposable fields should be disposed
        readonly IDisposable _scopeDisposer;
#pragma warning restore IDE0069 // Disposable fields should be disposed

        RootCache _cache;

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

        internal ComponentRegistration GetRegistrationFor<TService>() => _cache.Get<TService>().Registrations.Single();

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
        {
            Assert.State.Assert(!_disposed);
            if(!_createdServiceLocator)
            {
                _createdServiceLocator = true;
                _cache = new RootCache(_registeredComponents.Values.ToList());//Don't create in the constructor because no registrations are done and thus new component indexes will appear, thus breaking the cache.
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

        TService[] IServiceLocator.ResolveAll<TService>() => throw new NotImplementedException();

        IDisposable IServiceLocator.BeginScope()
        {
            Assert.State.Assert(!_disposed);
            if(_scopeCache.Value != null)
            {
                throw new Exception("Scope already exists. Nested scopes are not supported.");
            }

            _scopeCache.Value = _cache.CreateScopeCache();

            return _scopeDisposer;
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

        [ThreadStatic] static ComponentRegistration _parentComponent;
        public TService Resolve<TService>() where TService : class
        {
            Assert.State.Assert(!_disposed);
            var (registrations, instance) = _cache.Get<TService>();

            if(instance is TService singleton)
            {
                return singleton;
            }

            var scopeCache = _scopeCache.Value;

            // ReSharper disable once PatternAlwaysOfType Silly ReSharper is wrong again
            if (scopeCache != null && scopeCache.TryGet<TService>() is TService scoped)
            {
                return scoped;
            }

            if(registrations == null)
            {
                throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
            }

            if(registrations.Length > 1)
            {
                throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
            }

            var currentComponent = registrations[0];

            if(_parentComponent?.Lifestyle == Lifestyle.Singleton && currentComponent.Lifestyle != Lifestyle.Singleton)
            {
                throw new Exception($"{Lifestyle.Singleton} service: {_parentComponent.ServiceTypes.First().FullName} depends on {currentComponent.Lifestyle} service: {currentComponent.ServiceTypes.First().FullName} ");
            }

            var previousResolvingComponent = _parentComponent;
            _parentComponent = currentComponent;
            try
            {
                switch(currentComponent.Lifestyle)
                {
                    case Lifestyle.Singleton:
                    {
                        return (TService)currentComponent.GetSingletonInstance(this, _cache);
                    }
                    case Lifestyle.Scoped:
                    {
                        if(scopeCache == null)
                        {
                            throw new Exception("Attempted to resolve scoped component without a scope");
                        }
                        var newInstance = currentComponent.CreateInstance(this);
                        scopeCache.Set(newInstance, currentComponent);
                        return (TService)newInstance;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                _parentComponent = previousResolvingComponent;
            }
        }

        internal TService ResolveSingleton<TService>(ComponentRegistration currentComponent) where TService : class
        {
            Assert.State.Assert(!_disposed);
            var (registrations, instance) = _cache.Get<TService>();

            if(instance is TService singleton)
            {
                return singleton;
            }

            if(registrations.Length > 1)
            {
                throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
            }

            var previousResolvingComponent = _parentComponent;
            _parentComponent = currentComponent;
            try
            {
                return (TService)currentComponent.GetSingletonInstance(this, _cache);
            }
            finally
            {
                _parentComponent = previousResolvingComponent;
            }
        }

        internal TService ResolveScoped<TService>(ComponentRegistration currentComponent) where TService : class
        {
            Assert.State.Assert(!_disposed);
             var scopeCache = _scopeCache.Value;

            if(scopeCache == null)
            {
                throw new Exception("Attempted to resolve scoped component without a scope");
            }

            // ReSharper disable once PatternAlwaysOfType Silly ReSharper is wrong again
            if (scopeCache.TryGet<TService>() is TService scoped)
            {
                return scoped;
            }

            if(_parentComponent?.Lifestyle == Lifestyle.Singleton)
            {
                throw new Exception($"{Lifestyle.Singleton} service: {_parentComponent.ServiceTypes.First().FullName} depends on {currentComponent.Lifestyle} service: {currentComponent.ServiceTypes.First().FullName} ");
            }

            var previousResolvingComponent = _parentComponent;
            _parentComponent = currentComponent;
            try
            {
                var newInstance = currentComponent.CreateInstance(this);
                scopeCache.Set(newInstance, currentComponent);
                return (TService)newInstance;
            }
            finally
            {
                _parentComponent = previousResolvingComponent;
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
