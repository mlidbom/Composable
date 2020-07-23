using System;
using System.Collections.Generic;
using Composable.Contracts;
using JetBrains.Annotations;

namespace Composable.SystemCE.Linq
{
    /// <summary/>
    public static class Iteration
    {
        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
        {
            Contract.ArgumentNotNull(source, nameof(source), action, nameof(action));
            foreach(var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> source, [InstantHandle]Action<T> action)
        {
            Contract.ArgumentNotNull(source, nameof(source), action, nameof(action));

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
            Contract.ArgumentNotNull(source, nameof(source), action, nameof(action));

            var index = 0;
            foreach(var item in source)
            {
                action(item, index++);
            }
        }
    }
}