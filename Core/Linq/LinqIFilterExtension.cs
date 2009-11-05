using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class LinqIFilterExtension
    {
        public static IEnumerable<TItemType> Where<TItemType>(this IEnumerable<TItemType> source, IFilter<TItemType> filter)
        {
            foreach (var predicate in filter.Filters)
            {
                source = source.Where(predicate.Compile());
            }
            return source;
        }

        public static IQueryable<TItemType> Where<TItemType>(this IQueryable<TItemType> source, IFilter<TItemType> filter)
        {
            foreach (var predicate in filter.Filters)
            {
                source = source.Where(predicate);
            }
            return source;
        }
    }
}