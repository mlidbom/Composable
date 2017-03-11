#region usings

using System;
using System.Collections.Generic;


#endregion

namespace Composable.System.Linq
{
    /// <summary/>
    static class Hierarchy
    {
        /// <summary>
        /// Flattens a hierarchical structure of <typeparamref name="TSource"/> objects into an <see cref="IEnumerable{TSource}"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the objects in the hierarchy.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <param name="childrenSelector">A function that given a <typeparamref name="TSource"/> returns all the immediate descendent.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> containing all the <typeparamref name="TSource"/> instances in the <paramref name="source"/>.</returns>
        public static IEnumerable<TSource> FlattenHierarchy<TSource>(this IEnumerable<TSource> source,
                                                                     Func<TSource, IEnumerable<TSource>> childrenSelector)
        {
            foreach(var item in source)
            {
                foreach(var child in FlattenHierarchy(childrenSelector(item), childrenSelector))
                {
                    yield return child;
                }
                yield return item;
            }
        }
    }
}