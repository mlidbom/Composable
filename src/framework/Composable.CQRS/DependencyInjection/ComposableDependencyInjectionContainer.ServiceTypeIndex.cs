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
            internal static int ServiceCount { get; private set; }
            static readonly OptimizedThreadShared<Dictionary<Type, int>> Map = new OptimizedThreadShared<Dictionary<Type, int>>(new Dictionary<Type, int>());

            internal static int For(Type type) => Map.WithExclusiveAccess(map => map.GetOrAdd(type, () => ServiceCount++));

            internal static class ForService<TType>
            {
                internal static readonly int Index = ServiceTypeIndex.For(typeof(TType));
            }
        }
    }
}