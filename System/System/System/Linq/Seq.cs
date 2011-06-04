#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

#endregion

namespace Composable.System.Linq
{
    /// <summary/>
    [Pure]
    public static class Seq
    {
        /// <summary>
        /// Creates an enumerable consisting of the passed parameter values is order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<T> Create<T>(params T[] values)
        {
            return values;
        }


        public static IEnumerable<Type> OfTypes<T1>()
        {
            return Seq.Create(typeof(T1));
        }

        public static IEnumerable<Type> OfTypes<T1, T2>()
        {
            return OfTypes<T1>().Append(typeof(T2));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3>()
        {
            return OfTypes<T1, T2>().Append(typeof(T3));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4>()
        {
            return OfTypes<T1, T2, T3>().Append(typeof(T4));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5>()
        {
            return OfTypes<T1, T2, T3, T4>().Append(typeof(T5));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6>()
        {
            return OfTypes<T1, T2, T3, T4, T5>().Append(typeof(T6));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7>()
        {
            return OfTypes<T1, T2, T3, T4, T5, T6>().Append(typeof(T7));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>()
        {
            return OfTypes<T1, T2, T3, T4, T5, T6, T7>().Append(typeof(T8));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        {
            return OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>().Append(typeof(T9));
        }

        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        {
            return OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>().Append(typeof(T10));
        }
    }
}