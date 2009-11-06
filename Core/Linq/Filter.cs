using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class Filter
    {
        public static IEnumerable<TItemType> Where<TItemType>(this IEnumerable<TItemType> source, IFilter<TItemType> filter)
        {
            return filter.Filters.Aggregate(source, (aggregate, predicate) => aggregate.Where(predicate.Compile()));
        }

        public static IQueryable<TItemType> Where<TItemType>(this IQueryable<TItemType> source, IFilter<TItemType> filter)
        {
            return filter.Filters.Aggregate(source, (aggregate, predicate) => aggregate.Where(predicate));
        }

        public static bool Matches<T>(this IFilter<T> filter, T item)
        {
            return !filter.Filters.Any(predicate => !predicate.Compile().Invoke(item));
        }
    }
}