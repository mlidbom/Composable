using System;
using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;
using Composable.System.Linq;

namespace Composable.System.Collections.Collections
{
    ///<summary>Extensions on <see cref="ICollection{T}"/></summary>
    public static class CollectionExtensions
    {
        ///<summary>Remove entries matching the condition from the collection.</summary>
        public static int RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition)
        {
            ContractOptimized.Argument(me, nameof(me), condition, nameof(condition)).NotNull();
            var toRemove = me.Where(condition).ToList();
            toRemove.ForEach(removeMe => me.Remove(removeMe));
            return toRemove.Count;
        }

        ///<summary>Add all instances in <param name="toAdd"> to the collection <param name="me"></param>.</param></summary>
        public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd)
        {
            Contract.Argument(() => me, () => toAdd).NotNull();
            toAdd.ForEach(me.Add);
        }
    }
}
