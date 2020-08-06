using System;
using System.Collections.Generic;
using Composable.DependencyInjection;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable StaticMemberInGenericType

namespace Composable.SystemCE.ReflectionCE
{
    class TypeIndex<TInheritor> where TInheritor : TypeIndex<TInheritor>
    {
        static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
        internal static int ServiceCount { get; private set; }
        static IReadOnlyDictionary<Type, int> _map = new Dictionary<Type, int>();

        static Type[] _backMap = Array.Empty<Type>();

        internal static int For(Type type)
        {
            if(_map.TryGetValue(type, out var value))
                return value;

            using(Monitor.EnterUpdateLock())
            {
                if(_map.TryGetValue(type, out var value2))
                    return value2;

                ThreadSafe.AddToCopyAndReplace(ref _backMap, type);
                ThreadSafe.AddToCopyAndReplace(ref _map, type, ServiceCount++);
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
