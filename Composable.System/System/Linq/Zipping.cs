#region usings

using System;
using System.Collections.Generic;


#endregion

namespace Composable.System.Linq
{
    /// <summary/>
    public static class Zipping
    {
        /// <summary>
        /// Projects two sequences into a single sequence in which each value is the result
        /// of calling <paramref name="selector"/> on the two instances in <paramref name="first"/> and
        /// <paramref name="second"/> that are at that index.
        /// 
        /// The returned sequence will be the length of the shorter of the two sequences if
        /// they are uneven in length.
        /// </summary>
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
                                                                         Func<TFirst, TSecond, TResult> selector)
        {
            using(var firstEnum = first.GetEnumerator())
            {
                using(var secondEnum = second.GetEnumerator())
                {
                    while(firstEnum.MoveNext() && secondEnum.MoveNext())
                    {
                        yield return selector(firstEnum.Current, secondEnum.Current);
                    }
                }
            }
        }


        /// <summary>
        /// Projects two sequences into a single sequence in which each value is a <see cref="Pair{T,T2}"/>
        /// containing the two instances in <paramref name="first"/> and <paramref name="second"/> that are at that index.
        /// 
        /// The returned sequence will be the length of the shorter of the two sequences if
        /// they are uneven in length.
        /// </summary>
        public static IEnumerable<Pair<TFirst, TSecond>> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            return first.Zip(second, (fst, snd) => new Pair<TFirst, TSecond>(fst, snd));
        }

        /// <summary>
        /// A simple class that represents the pairing of two instances of the same type:
        /// <see cref="First"/> and <see cref="Second"/>
        /// </summary>
        public class Pair<T, T2>
        {
            /// <summary>The first instance in the pair.</summary>
            public T First { get; set; }

            /// <summary>The second instance in the pair.</summary>
            public T2 Second { get; set; }

            /// <summary>Constructs a pair.</summary>
            public Pair(T first, T2 second)
            {
                First = first;
                Second = second;
            }

            /// <summary><see cref="object.ToString"/></summary>
            public override string ToString()
            {
                return string.Format("({0}, {1})", First, Second);
            }
        }
    }
}