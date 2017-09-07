using System;
using System.Collections.Generic;
using Composable.Testing.Contracts;

namespace Composable.Testing.System.Linq
{
    /// <summary/>
    static class Iteration
    {
        /// <summary>
        /// Executes <paramref name="action"/> for each element in the sequence <paramref name="source"/>.
        /// </summary>
        public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
        {
            ContractOptimized.Argument(source, nameof(source), action, nameof(action))
                             .NotNull();
            foreach(var item in source)
            {
                action(item);
            }
        }
    }
}