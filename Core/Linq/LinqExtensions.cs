using System;
using System.Collections.Generic;
using System.Linq;

namespace Void.Linq
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] instances)
        {
            return source.Concat(instances);
        }       

        public static void ForEach<TSource, TReturn>(this IEnumerable<TSource> source, Func<TSource, TReturn> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }

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
        }
    }
}