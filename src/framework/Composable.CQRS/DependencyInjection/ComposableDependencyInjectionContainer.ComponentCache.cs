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

            internal RootCache(IReadOnlyList<ComponentRegistration> registrations) : this(CreateArrays(registrations))
            {
            }

            internal ScopeCache Clone() => new ScopeCache(_serviceTypeIndexToComponentIndex);

            public void Set(object instance, ComponentRegistration registration) => _instances[registration.ComponentIndex] = instance;

            internal TService TryGet<TService>() => (TService)_instances[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal ComponentRegistration[] GetRegistration<TService>() => _components[ServiceTypeIndex.ForService<TService>.Index];

            RootCache((ComponentRegistration[][], int[]) arrays)
            {
                _components = arrays.Item1;
                _serviceTypeIndexToComponentIndex = arrays.Item2;
                _instances = new object[_components.Length];
            }

            static (ComponentRegistration[][], int[]) CreateArrays(IReadOnlyList<ComponentRegistration> registrations)
            {
               var componentArray = new ComponentRegistration[ServiceTypeIndex.ServiceCount][];
                var typeToComponentIndex = new int[ServiceTypeIndex.ServiceCount];

                registrations.SelectMany(registration => registration.ServiceTypeIndexes.Select(typeIndex => new {registration, typeIndex}))
                             .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                             .ForEach(registrationsOnTypeindex => componentArray[registrationsOnTypeindex.Key] = registrationsOnTypeindex.Select(regs => regs.registration).ToArray());


                foreach (var registration in registrations)
                {
                    foreach (var serviceTypeIndex in registration.ServiceTypeIndexes)
                    {
                        typeToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
                    }
                }

                return (componentArray, typeToComponentIndex);
            }
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