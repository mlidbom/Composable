using System;
using System.Collections.Generic;
using Composable.System.Collections.Collections;
using Composable.System.Threading.ResourceAccess;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal static class ServiceTypeIndex
        {
            internal static int ComponentCount { get; private set; }
            static readonly OptimizedThreadShared<Dictionary<Type, int>> Map = new OptimizedThreadShared<Dictionary<Type, int>>(new Dictionary<Type, int>());

            internal static int For(Type type) => Map.WithExclusiveAccess(map => map.GetOrAdd(type, () => ComponentCount++));
            internal static int For<TType>() => CacheForType<TType>.Index;

            static class CacheForType<TType>
            {
                internal static readonly int Index = ServiceTypeIndex.For(typeof(TType));
            }

            public static void InitAll(IReadOnlyList<ComponentRegistration> registrations)
            {
                foreach(var registration in registrations)
                {
                    foreach(var serviceType in registration.ServiceTypes)
                    {
                        For(serviceType);
                    }
                }
            }
        }
    }
}