using System;
using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] instances)
        {
            return source.Concat(instances);
        }           
    }
}