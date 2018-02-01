using System.Collections.Generic;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal class ComponentCache
        {
            readonly ComponentRegistration[] _components;
            readonly object[] _instances;

            internal ComponentCache(IReadOnlyList<ComponentRegistration> registrations) : this(CreateComponentArray(registrations))
            {
            }

            internal ComponentCache Clone() => new ComponentCache(_components);

            public void Set(object instance, ComponentRegistration registration) => _instances[registration.ComponentIndex] = instance;
            internal object TryGet<TService>() => _instances[ServiceTypeIndex.For<TService>()];

            internal ComponentRegistration GetRegistration<TService>() => _components[ServiceTypeIndex.For<TService>()];

            ComponentCache(ComponentRegistration[] components)
            {
                _components = components;
                _instances = new object[_components.Length];
            }

            static ComponentRegistration[] CreateComponentArray(IReadOnlyList<ComponentRegistration> registrations)
            {
                ServiceTypeIndex.InitAll(registrations);
                var componentArray = new ComponentRegistration[ServiceTypeIndex.ComponentCount];
                foreach(var registration in registrations)
                {
                    componentArray[registration.ComponentIndex] = registration;
                }

                return componentArray;
            }
        }
    }
}