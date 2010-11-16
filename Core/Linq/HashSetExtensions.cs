using System.Collections.Generic;

namespace Composable.System.Linq
{
    public static class HashSetExtensions
    {
        public static ISet<T> ToSet<T>(this IEnumerable<T> me)
        {
            return new HashSet<T>(me);
        }
    }
}