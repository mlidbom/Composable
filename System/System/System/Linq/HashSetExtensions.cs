#region usings

using System.Collections;
using System.Collections.Generic;

#endregion

namespace Composable.System.Linq
{
    /// <summary>A collection of extensions to work with <see cref="HashSet{T}"/></summary>
    public static class HashSetExtensions
    {
        /// <returns>A set containing all the items in <paramref name="me"/></returns>
        public static HashSet<T> ToSet<T>(this IEnumerable<T> me)
        {
            return new HashSet<T>(me);
        }

        public static void AddOrReplace<T>(this ISet<T> me, T value)
        {
            me.Remove(value);
            me.Add(value);
        }

        public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> value)
        {
            value.ForEach(me.Add);
        }
    }
}