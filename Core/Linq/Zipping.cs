using System;
using System.Collections.Generic;

namespace Void.Linq
{
    /// <summary/>
    public static class Zipping
    {
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> selector)
        {
            using (var firstEnum = first.GetEnumerator())
            using (var secondEnum = second.GetEnumerator())
            {
                while (firstEnum.MoveNext() && secondEnum.MoveNext())
                {
                    yield return selector(firstEnum.Current, secondEnum.Current);
                }
            }
        }


        public static IEnumerable<Pair<TFirst, TSecond>> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            return first.Zip(second, (fst, snd) => new Pair<TFirst, TSecond>(fst, snd));
        }

        public class Pair<T, T2>
        {
            public T First { get; set; }
            public T2 Second { get; set; }

            public Pair(T first, T2 second)
            {
                First = first;
                Second = second;
            }

            public override string ToString()
            {
                return string.Format("({0}, {1})", First, Second);
            }
        }
    }
}