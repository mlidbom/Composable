using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Void.Linq
{
    ///<summary>
    /// Methods useful for any type when used in a Linq context
    ///</summary>
    [Pure]
    public static class ObjectExtensions
    {
        /// <summary>
        /// Returns <paramref name="me"/> repeated <paramref name="times"/> times.
        /// </summary>
        public static IEnumerable<T> Repeat<T>(this T me, int times)
        {
            while (times-- > 0)
            {
                yield return me;
            }
        }
    }
}