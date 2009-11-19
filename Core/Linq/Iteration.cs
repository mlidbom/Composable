using System;
using System.Collections.Generic;

namespace Void.Linq
{
    /// <summary/>
    public static class Iteration
    {
        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
        {
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
            int index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }
    }
}