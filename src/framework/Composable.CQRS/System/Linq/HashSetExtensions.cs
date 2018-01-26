using System.Collections.Generic;

using Composable.Contracts;

namespace Composable.System.Linq
{
    /// <summary>A collection of extensions to work with <see cref="HashSet{T}"/></summary>
    static class HashSetExtensions
    {
        /// <returns>A set containing all the items in <paramref name="me"/></returns>
        public static HashSet<T> ToSet<T>(this IEnumerable<T> me)
        {
            Contract.Argument(() => me).NotNull();
            return new HashSet<T>(me);
        }

        ///<summary>
        /// Removes all of the items in the supplied enumerable from the set.
        /// Simply forwards to ExceptWith but providing a name that is not utterly unreadable </summary>
        public static void RemoveRange<T>(this ISet<T> me, IEnumerable<T> toRemove)
        {
            Contract.Argument(() => me, () => toRemove).NotNull();
            me.ExceptWith(toRemove);
        }

        ///<summary>Adds all the supplied <paramref name="toAdd"/> instances to the set.</summary>
        public static void AddRange<T>(this ISet<T> me, IEnumerable<T> toAdd)
        {
            Contract.Argument(() => me, () => toAdd).NotNull();
            toAdd.ForEach(me.Add);
        }
    }
}