using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE.ThreadingCE;

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

        public static async Task<T> MutateAsync<T>(this T @this, Func<T, Task> mutate)
        {
            await mutate(@this).NoMarshalling();
            return @this;
        }

        public static TResult MapTo<TValue, TResult>(this TValue @this, Func<TValue, TResult> transform) => transform(@this);

        public static string ToStringNotNull(this object @this) => Contract.ReturnNotNull(@this.ToString());
    }
}