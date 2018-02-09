using System;
using System.Collections.Generic;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal static class ServiceTypeIndex
        {
            internal static readonly object Lock = new object();
            internal static int ServiceCount { get; private set; }
            static Dictionary<Type, int> _map = new Dictionary<Type, int>();

            internal static int For(Type type)
            {
                if(_map.TryGetValue(type, out var value))
                    return value;

                lock(Lock)
                {
                    if(_map.TryGetValue(type, out var value2))
                        return value2;

                    var newMap = new Dictionary<Type, int>(_map) {{type, ServiceCount++}};
                    _map = newMap;
                    return ServiceCount - 1;
                }
            }

            internal static class ForService<TType>
            {
                internal static readonly int Index = ServiceTypeIndex.For(typeof(TType));
            }
        }
    }
}