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

            internal object TryGet<TService>() => _instances[ComponentIndex.For<TService>()];

            ComponentCache(ComponentRegistration[] components)
            {
                _components = components;
                _instances = new object[_components.Length];
            }

            static ComponentRegistration[] CreateComponentArray(IReadOnlyList<ComponentRegistration> registrations)
            {
                ComponentIndex.InitAll(registrations);
                var array = new ComponentRegistration[ComponentIndex.ComponentCount];
                foreach(var registration in registrations)
                {
                    array[registration.ComponentIndex] = registration;
                }

                return array;
            }
        }
    }
}