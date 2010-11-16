using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Composable.System.Linq
{
    /// <summary/>
    [Pure]
    public static class Seq
    {
        /// <summary>
        /// Creates an enumerable consisting of the passed parameter values is order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<T> Create<T>(params T[] values)
        {
            return values;
        }
    }
}