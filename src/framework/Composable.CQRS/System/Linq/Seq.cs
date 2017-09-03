using System;
using System.Collections.Generic;
using Composable.Contracts;
// ReSharper disable MemberCanBePrivate.Global

namespace Composable.System.Linq
{


    /// <summary/>
    static class Seq
    {
        static class EmptySequence<T>
        {
            public static readonly IEnumerable<T> Instance = new T[0];
        }

        /// <summary>Returns an empty array of type T. Does not allocate any memory unless this is the first time it is called for T. </summary>
        public static IEnumerable<T> Empty<T>() => EmptySequence<T>.Instance;

        /// <summary>
        /// Creates an enumerable consisting of the passed parameter values is order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<T> Create<T>(params T[] values)
        {
            ContractOptimized.Argument(values, nameof(values))
                             .NotNull();
            return values;
        }

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1>() => Create(typeof(T1));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2>() => OfTypes<T1>().Append(typeof(T2));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3>() => OfTypes<T1, T2>().Append(typeof(T3));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4>() => OfTypes<T1, T2, T3>().Append(typeof(T4));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5>() => OfTypes<T1, T2, T3, T4>().Append(typeof(T5));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6>() => OfTypes<T1, T2, T3, T4, T5>().Append(typeof(T6));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7>() => OfTypes<T1, T2, T3, T4, T5, T6>().Append(typeof(T7));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>() => OfTypes<T1, T2, T3, T4, T5, T6, T7>().Append(typeof(T8));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>().Append(typeof(T9));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>().Append(typeof(T10));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>().Append(typeof(T11));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>().Append(typeof(T12));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>().Append(typeof(T13));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>().Append(typeof(T14));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>().Append(typeof(T15));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>().Append(typeof(T16));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>().Append(typeof(T17));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>().Append(typeof(T18));

        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>().Append(typeof(T19));

        // ReSharper disable once UnusedMember.Global
        ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
        public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>().Append(typeof(T20));
    }
}