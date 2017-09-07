using System;
using System.Collections.Generic;
using Composable.Testing.Contracts;

namespace Composable.Testing.System.Linq
{
    /// <summary/>
    public static class Iteration
    {
        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
        {
            Contract.AssertThat(source != null && action != null);
            foreach(var item in source)
            {
                action(item);
            }
        }
    }
}