using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Linq;

namespace Composable.System.Collections.Collections
{
    ///<summary>Extensions on <see cref="ICollection{T}"/></summary>
    public static class CollectionExtensions
    {
        ///<summary>Remove entries matching the condition from the collection.</summary>
        public static int RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition)
        {
            Contract.Requires(me != null && condition != null);
            var toRemove = me.Where(condition).ToList();
            toRemove.ForEach(removeMe => me.Remove(removeMe));
            return toRemove.Count;
        }

        ///<summary>Remove all the instances in <param name="toRemove"/> from the collection <param name="me"></param></summary>
        public static void RemoveRange<T>(this ICollection<T> me, IEnumerable<T> toRemove)
        {
            Contract.Requires(me != null && toRemove != null);
            toRemove.ForEach(removeMe => me.Remove(removeMe));
        }

        ///<summary>Add all instances in <param name="toAdd"> to the collection <param name="me"></param>.</param></summary>
        public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd)
        {
            Contract.Requires(me != null && toAdd != null);
            toAdd.ForEach(me.Add);
        }
    }
}
