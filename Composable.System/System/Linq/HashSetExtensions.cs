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

        ///<summary>
        /// Removes all of the items in the supplied enumerable from the set.
        /// Simply forwards to ExceptWith but providing a name that is not utterly unreadable </summary>
        public static void RemoveRange<T>(this ISet<T> me, IEnumerable<T> value)
        {
            me.ExceptWith(value);
        }

        public static void AddRange<T>(this ISet<T> me, IEnumerable<T> value)
        {
            value.ForEach(toAdd => me.Add(toAdd));
        }
    }
}