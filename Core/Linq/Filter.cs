using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    /// <summary/>
    public static class Filter
    {
        /// <summary>
        /// Returns an IEnumerable containing only those items in <paramref name="source"/> 
        /// which match all the predicates in <paramref name="filter"/>
        /// </summary>
        public static IEnumerable<TItemType> Where<TItemType>(this IEnumerable<TItemType> source, IFilter<TItemType> filter)
        {
            return filter.Filters.Aggregate(source, (aggregate, predicate) => aggregate.Where(predicate.Compile()));
        }

        /// <summary>
        /// Returns an IEnumerable containing only those items in <paramref name="source"/> 
        /// which match all the criteria in <paramref name="filter"/>
        /// </summary>
        public static IQueryable<TItemType> Where<TItemType>(this IQueryable<TItemType> source, IFilter<TItemType> filter)
        {
            return filter.Filters.Aggregate(source, (aggregate, predicate) => aggregate.Where(predicate));
        }

        /// <summary>
        /// returns true if <paramref name="item"/> matches all the predicates in <paramref name="filter"/>
        /// </summary>
        public static bool Matches<T>(this IFilter<T> filter, T item)
        {
            return !filter.Filters.Any(predicate => !predicate.Compile().Invoke(item));
        }
    }
}