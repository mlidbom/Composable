using System;
using System.Collections.Generic;
using Composable.DependencyInjection;
using Composable.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable StaticMemberInGenericType

namespace Composable.SystemCE.ReflectionCE
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

                _backMap = _backMap.AddToCopy(type);
                _map = _map.AddToCopy(type, ServiceCount++);
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
