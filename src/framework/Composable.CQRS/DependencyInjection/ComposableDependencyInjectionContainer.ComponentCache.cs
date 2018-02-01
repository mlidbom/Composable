using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal class ComponentCache
        {
            readonly ComponentRegistration[][] _components;
            readonly object[] _instances;

            internal ComponentCache(IReadOnlyList<ComponentRegistration> registrations) : this(CreateComponentArray(registrations))
            {
            }

            internal ComponentCache Clone() => new ComponentCache(_components);

            public void Set(object instance, ComponentRegistration registration) => _instances[registration.ComponentIndex] = instance;
            internal object TryGet<TService>() => _instances[ServiceTypeIndex.For<TService>()];

            internal ComponentRegistration[] GetRegistration<TService>() => _components[ServiceTypeIndex.For<TService>()];

            ComponentCache(ComponentRegistration[][] components)
            {
                _components = components;
                _instances = new object[_components.Length];
            }

            static ComponentRegistration[][] CreateComponentArray(IReadOnlyList<ComponentRegistration> registrations)
            {
               var componentArray = new ComponentRegistration[ServiceTypeIndex.ComponentCount][];

                registrations.SelectMany(registration => registration.ServiceTypeIndexes.Select(typeIndex => new {registration, typeIndex}))
                             .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                             .ForEach(registrationsOnTypeindex => componentArray[registrationsOnTypeindex.Key] = registrationsOnTypeindex.Select(regs => regs.registration).ToArray());

                return componentArray;
            }
        }
    }
}