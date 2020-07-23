using System.Collections.Generic;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;

namespace Composable.SystemCE.CollectionsCE
{
    /// <summary>A collection of extensions to work with <see cref="HashSet{T}"/></summary>
    static class HashSetCE
    {
        /// <returns>A set containing all the items in <paramref name="me"/></returns>
        public static HashSet<T> ToSet<T>(this IEnumerable<T> me)
        {
            Contract.ArgumentNotNull(me, nameof(me));
            return new HashSet<T>(me);
        }

        ///<summary>
        /// Removes all of the items in the supplied enumerable from the set.
        /// Simply forwards to ExceptWith but providing a name that is not utterly unreadable </summary>
        public static void RemoveRange<T>(this ISet<T> me, IEnumerable<T> toRemove)
        {
            Contract.ArgumentNotNull(me, nameof(me), toRemove, nameof(toRemove));
            me.ExceptWith(toRemove);
        }

        ///<summary>Adds all the supplied <paramref name="toAdd"/> instances to the set.</summary>
        public static void AddRange<T>(this ISet<T> me, IEnumerable<T> toAdd)
        {
            Contract.ArgumentNotNull(me, nameof(me),toAdd, nameof(toAdd));
            toAdd.ForEach(me.Add);
        }
    }
}