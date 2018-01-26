using System;
using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;
using Composable.System.Linq;

namespace Composable.System.Collections.Collections
{
    ///<summary>Extensions on <see cref="ICollection{T}"/></summary>
    static class CollectionExtensions
    {
        ///<summary>Remove entries matching the condition from the collection.</summary>
        public static IReadOnlyList<T> RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition)
        {
            ContractOptimized.Argument(me, nameof(me), condition, nameof(condition)).NotNull();
            var removed = me.Where(condition).ToList();
            removed.ForEach(removeMe => me.Remove(removeMe));
            return removed;
        }

        ///<summary>Add all instances in <param name="toAdd"> to the collection <param name="me"></param>.</param></summary>
        public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toAdd)
        {
            Contract.Argument(() => me, () => toAdd).NotNull();
            toAdd.ForEach(me.Add);
        }
    }
}
