using System;
using System.Collections.Generic;
using Composable.Contracts;

namespace Composable.SystemCE.LinqCE
{
    ///<summary>
    /// Methods useful for any type when used in a Linq context
    ///</summary>
    public static class ObjectCE
    {
        /// <summary>
        /// Returns <paramref name="me"/> repeated <paramref name="times"/> times.
        /// </summary>
        internal static IEnumerable<T> Repeat<T>(this T me, int times)
        {
            while(times-- > 0)
            {
                yield return me;
            }
        }


        public static T Mutate<T>(this T @this, Action<T> mutate)
        {
            mutate(@this);
            return @this;
        }

        public static TResult MapTo<TValue, TResult>(this TValue @this, Func<TValue, TResult> transform) => transform(@this);

        public static string ToStringNotNull(this object @this) => Contract.ReturnNotNull(@this.ToString());
    }
}