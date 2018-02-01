using System.Collections.Generic;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal class ComponentCache
        {
            readonly ComponentRegistration[] _components;
            readonly object[] _instances;

            internal ComponentCache(IEnumerable<ComponentRegistration> registrations) : this(CreateComponentArray(registrations))
            {
            }

            internal ComponentCache Clone() => new ComponentCache(_components);

            ComponentCache(ComponentRegistration[] components)
            {
                _components = components;
                _instances = new object[_components.Length];
            }

            static ComponentRegistration[] CreateComponentArray(IEnumerable<ComponentRegistration> registrations)
            {
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