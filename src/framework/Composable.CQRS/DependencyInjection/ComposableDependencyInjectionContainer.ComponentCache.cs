using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal class ComponentCache : IDisposable
        {
            internal bool IsDisposed;
            readonly ComponentRegistration[][] _components;
            readonly int[] _typeIndexToComponentIndex;
            readonly object[] _instances;
            readonly LinkedList<IDisposable> _disposables = new LinkedList<IDisposable>();
            readonly Lifestyle[] _serviceLifestyles;

            internal ComponentCache(IReadOnlyList<ComponentRegistration> registrations) : this(CreateArrays(registrations))
            {
            }

            internal ScopeCache Clone() => new ScopeCache((_typeIndexToComponentIndex, _serviceLifestyles));

            public void Set(object instance, ComponentRegistration registration)
            {
                _instances[registration.ComponentIndex] = instance;
                if(instance is IDisposable disposable)
                {
                    _disposables.AddLast(disposable);
                }
            }

            internal Lifestyle GetLifeStyle<TService>() => _serviceLifestyles[ServiceTypeIndex.ForService<TService>.Index];

            internal TService TryGet<TService>() => (TService)_instances[_typeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal ComponentRegistration[] GetRegistration<TService>() => _components[ServiceTypeIndex.ForService<TService>.Index];

            ComponentCache((ComponentRegistration[][], int[], Lifestyle[]) arrays)
            {
                _components = arrays.Item1;
                _typeIndexToComponentIndex = arrays.Item2;
                _serviceLifestyles = arrays.Item3;
                _instances = new object[_components.Length];
            }

            static (ComponentRegistration[][], int[], Lifestyle[]) CreateArrays(IReadOnlyList<ComponentRegistration> registrations)
            {
               var componentArray = new ComponentRegistration[ServiceTypeIndex.ServiceCount][];
                var typeToComponentIndex = new int[ServiceTypeIndex.ServiceCount];
                var serviceLifeStyles = new Lifestyle[ServiceTypeIndex.ServiceCount];

                registrations.SelectMany(registration => registration.ServiceTypeIndexes.Select(typeIndex => new {registration, typeIndex}))
                             .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                             .ForEach(registrationsOnTypeindex => componentArray[registrationsOnTypeindex.Key] = registrationsOnTypeindex.Select(regs => regs.registration).ToArray());


                foreach (var registration in registrations)
                {
                    foreach (var serviceTypeIndex in registration.ServiceTypeIndexes)
                    {
                        typeToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
                        serviceLifeStyles[serviceTypeIndex] = registration.Lifestyle;
                    }
                }

                return (componentArray, typeToComponentIndex, serviceLifeStyles);
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

        internal class ScopeCache : IDisposable
        {
            internal bool IsDisposed;
            readonly int[] _typeIndexToComponentIndex;
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

            internal TService TryGet<TService>() => (TService)_instances[_typeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal ScopeCache((int[], Lifestyle[]) arrays)
            {
                _typeIndexToComponentIndex = arrays.Item1;
                _instances = new object[_typeIndexToComponentIndex.Length];
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