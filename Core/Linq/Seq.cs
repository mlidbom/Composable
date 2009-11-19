using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    /// <summary/>
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