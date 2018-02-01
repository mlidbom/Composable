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
            readonly int[] _typeIndexToComponentIndex;
            readonly object[] _instances;

            internal ComponentCache(IReadOnlyList<ComponentRegistration> registrations) : this(CreateComponentArray(registrations), CreateTypeToComponentIndex(registrations))
            {
            }


            static int[] CreateTypeToComponentIndex(IReadOnlyList<ComponentRegistration> registrations)
            {
                var typeToComponentIndex = new int[ServiceTypeIndex.ComponentCount];
                foreach(var registration in registrations)
                {
                    foreach(var serviceTypeIndex in registration.ServiceTypeIndexes)
                    {
                        typeToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
                    }
                }

                return typeToComponentIndex;
            }

            internal ComponentCache Clone() => new ComponentCache(_components, _typeIndexToComponentIndex);

            public void Set(object instance, ComponentRegistration registration) => _instances[registration.ComponentIndex] = instance;

            internal object TryGet<TService>() => _instances[_typeIndexToComponentIndex[ServiceTypeIndex.For<TService>()]];

            internal ComponentRegistration[] GetRegistration<TService>() => _components[ServiceTypeIndex.For<TService>()];

            ComponentCache(ComponentRegistration[][] components, int[] typeIndexToComponentIndex)
            {
                _components = components;
                _typeIndexToComponentIndex = typeIndexToComponentIndex;
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