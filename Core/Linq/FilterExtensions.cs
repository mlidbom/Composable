using System.Linq;

namespace Void.Linq
{
    public static class FilterExtensions
    {
        public static bool Matches<T>(this IFilter<T> filter, T item)
        {
            return !filter.Filters.Any(predicate => !predicate.Compile().Invoke(item));
        }
    }
}