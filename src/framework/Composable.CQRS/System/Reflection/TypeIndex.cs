using System;
using System.Collections.Immutable;
using Composable.DependencyInjection;
// ReSharper disable StaticMemberInGenericType

namespace Composable.System.Reflection
{
    class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
    {
        static readonly object Lock = new object();
        internal static int ServiceCount { get; private set; }
        static ImmutableDictionary<Type, int> _map = ImmutableDictionary<Type, int>.Empty;

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
                _map = _map.Add(type, ServiceCount++);
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
