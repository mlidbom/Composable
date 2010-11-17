using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Composable.System.Linq
{
    /// <summary/>
    public static class Iteration
    {
        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
        {
            Contract.Requires(source != null && action != null);            
            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            Contract.Requires(source != null && action != null);
            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            Contract.Requires(source != null && action != null);
            var index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }
    }
}