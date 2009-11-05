using System;
using System.Collections.Generic;

namespace Void.Linq
{
    public static class Hierarchy
    {
        public static IEnumerable<TSource> FlattenHierarchy<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TSource>> childrenSelector)
        {
            foreach (var item in source)
            {                
                foreach (var child in FlattenHierarchy(childrenSelector(item), childrenSelector))
                {
                    yield return child;
                }
                yield return item;
            }
        }
    }
}