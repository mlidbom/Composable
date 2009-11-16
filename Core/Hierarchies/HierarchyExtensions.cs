using System;
using System.Collections.Generic;
using Void.Linq;

namespace Void.Hierarchies
{
    public static class HierarchyExtensions
    {
        public static IEnumerable<T> FlattenHierarchy<T>(this T me, Func<T, IEnumerable<T>> childSelector)
        {
            return Seq.Create(me).FlattenHierarchy(childSelector);
        }
    }
}