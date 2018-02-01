using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        class RootCache
        {
            readonly ComponentRegistration[][] _components;
            readonly int[] _serviceTypeIndexToComponentIndex;
            readonly object[] _instances;

            readonly (ComponentRegistration[] Registrations, object Instance)[] _cache;

            internal RootCache(IReadOnlyList<ComponentRegistration> registrations)
            {
                var serviceCount = ServiceTypeIndex.ServiceCount;

                _components = new ComponentRegistration[serviceCount][];
                _serviceTypeIndexToComponentIndex = new int[serviceCount];
                _instances = new object[serviceCount];
                _cache = new (ComponentRegistration[] Registrations, object Instance)[serviceCount];

                registrations.SelectMany(registration => registration.ServiceTypeIndexes.Select(typeIndex => new {registration, typeIndex}))
                             .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                             .ForEach(registrationsOnTypeindex =>
                              {
                                  _cache[registrationsOnTypeindex.Key].Registrations = registrationsOnTypeindex.Select(regs => regs.registration).ToArray();
                                  _components[registrationsOnTypeindex.Key] = registrationsOnTypeindex.Select(regs => regs.registration).ToArray();
                              });


                foreach (var registration in registrations)
                {
                    foreach (var serviceTypeIndex in registration.ServiceTypeIndexes)
                    {
                        _serviceTypeIndexToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
                    }
                }
            }

            internal ScopeCache Clone() => new ScopeCache(_serviceTypeIndexToComponentIndex);

            public void Set(object instance, ComponentRegistration registration) => _cache[registration.ComponentIndex].Instance = instance;

            internal (ComponentRegistration[] Registrations, object Instance) Get<TService>() => _cache[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal TService TryGet<TService>() => (TService)_instances[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal ComponentRegistration[] GetRegistration<TService>() => _components[ServiceTypeIndex.ForService<TService>.Index];
        }

        class ScopeCache : IDisposable
        {
            internal bool IsDisposed;
            readonly int[] _serviceTypeIndexToComponentIndex;
            readonly object[] _instances;
            readonly LinkedList<IDisposable> _disposables = new LinkedList<IDisposable>();

            public void Set(object instance, ComponentRegistration registration)
            {
                _instances[registration.ComponentIndex] = instance;
                if(instance is IDisposable disposable)
                {
                    _disposables.AddLast(disposable);
                }
            }

            internal TService TryGet<TService>() => (TService)_instances[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal ScopeCache(int[] serviceServiceTypeToComponentIndex)
            {
                _serviceTypeIndexToComponentIndex = serviceServiceTypeToComponentIndex;
                _instances = new object[serviceServiceTypeToComponentIndex.Length];
            }


            public void Dispose()
            {
                if(!IsDisposed)
                {
                    IsDisposed = true;
                    foreach (var disposable in _disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}