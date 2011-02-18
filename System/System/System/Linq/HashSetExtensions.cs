#region usings

using System.Collections.Generic;

#endregion

namespace Composable.System.Linq
{
    /// <summary>A collection of extensions to work with <see cref="HashSet{T}"/></summary>
    public static class HashSetExtensions
    {
        /// <returns>A set containing all the items in <paramref name="me"/></returns>
        public static ISet<T> ToSet<T>(this IEnumerable<T> me)
        {
            return new HashSet<T>(me);
        }
    }
}