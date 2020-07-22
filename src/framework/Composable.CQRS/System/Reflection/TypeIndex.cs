using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Composable.DependencyInjection;
// ReSharper disable StaticMemberInGenericType

namespace Composable.System.Reflection
{
    class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
    {
        static readonly object Lock = new object();
        internal static int ServiceCount { get; private set; }
        static IReadOnlyDictionary<Type, int> _map = new Dictionary<Type, int>();

        static Type[] _backMap = Array.Empty<Type>();

        internal static int For(Type type)
        {
            if(_map.TryGetValue(type, out var value))
                return value;

            lock(Lock)
            {
                if(_map.TryGetValue(type, out var value2))
                    return value2;

                var newBackMap = new Type[_backMap.Length + 1];
                Array.Copy(_backMap, newBackMap, _backMap.Length);
                newBackMap[^1] = type;
                _backMap = newBackMap;
                _map = new Dictionary<Type, int>(_map) {{type, ServiceCount++}};
                return ServiceCount - 1;
            }
        }

        internal static class ForService<TType>
        {
            internal static readonly int Index = ComposableDependencyInjectionContainer.ServiceTypeIndex.For(typeof(TType));
        }

        public static Type GetServiceForIndex(int serviceTypeIndex) => _backMap[serviceTypeIndex];
    }
}
