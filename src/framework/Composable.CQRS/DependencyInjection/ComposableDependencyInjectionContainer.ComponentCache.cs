using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        class RootCache : IDisposable
        {
            readonly int[] _serviceTypeIndexToComponentIndex;
            readonly (ComponentRegistration[] Registrations, object Instance)[] _cache;

            internal RootCache(IReadOnlyList<ComponentRegistration> registrations)
            {
                var serviceCount = ServiceTypeIndex.ServiceCount;

                _serviceTypeIndexToComponentIndex = new int[serviceCount + 1];
                for(var index = 0; index < _serviceTypeIndexToComponentIndex.Length; index++)
                {
                    _serviceTypeIndexToComponentIndex[index] = serviceCount;
                }
                _cache = new (ComponentRegistration[] Registrations, object Instance)[serviceCount + 1];

                registrations.SelectMany(registration => registration.ServiceTypes.Select(serviceType => new {registration, serviceType, typeIndex = ServiceTypeIndex.For(serviceType)}))
                             .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                             .ForEach(registrationsOnTypeIndex =>
                              {
                                  //refactor: We don't support more than one registration. The whole DI container assumes a single registration. Why does this code not?
                                  _cache[registrationsOnTypeIndex.Key].Registrations = registrationsOnTypeIndex.Select(regs => regs.registration).ToArray();
                              });


                foreach (var registration in registrations)
                {
                    foreach (var serviceTypeIndex in registration.ServiceTypeIndexes)
                    {
                        if(_serviceTypeIndexToComponentIndex[serviceTypeIndex] != serviceCount)
                        {
                            throw new Exception($"Already has a component registered for service: {ServiceTypeIndex.GetServiceForIndex(serviceTypeIndex)}");
                        }
                        _serviceTypeIndexToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
                    }
                }
            }

            internal ScopeCache CreateScopeCache() => new ScopeCache(_serviceTypeIndexToComponentIndex);

            public void Set(object instance, ComponentRegistration registration) => _cache[registration.ComponentIndex].Instance = instance;

            internal (ComponentRegistration[] Registrations, object Instance) TryGet<TService>() => _cache[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            public void Dispose()
            {
                // ReSharper disable ConditionIsAlwaysTrueOrFalse
                DisposeComponents(_cache.Where(@this => @this.Registrations != null && @this.Instance != null)
                                         // ReSharper restore ConditionIsAlwaysTrueOrFalse
                                        .Where(@this => @this.Registrations[0].InstantiationSpec.SingletonInstance == null) //We don't dispose instance registrations.
                                        .Select(@this => @this.Instance)
                                        .OfType<IDisposable>());
            }
        }

        internal class ScopeCache : IDisposable
        {
            bool _isDisposed;
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

            internal bool TryGet<TService>([MaybeNullWhen(false)]out TService service)
            {
                service = (TService)_instances[_serviceTypeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];
                return !Equals(service, default);
            }

            internal ScopeCache(int[] serviceServiceTypeToComponentIndex)
            {
                _serviceTypeIndexToComponentIndex = serviceServiceTypeToComponentIndex;
                _instances = new object[serviceServiceTypeToComponentIndex.Length];
            }


            public void Dispose()
            {
                if(!_isDisposed)
                {
                    _isDisposed = true;
                    DisposeComponents(_disposables);
                }
            }
        }

        static void DisposeComponents(IEnumerable<IDisposable> disposables)
        {
            var exceptions = disposables
                            .Select(disposable => ExceptionCE.TryCatch(disposable.Dispose))
                            .Where(me => me != null)
                             // ReSharper disable once RedundantEnumerableCastCall
                            .Cast<Exception>()
                            .ToList();

            if(exceptions.Any())
            {
                throw new AggregateException("Exceptions where thrown in Dispose methods of components", exceptions);
            }
        }
    }
}