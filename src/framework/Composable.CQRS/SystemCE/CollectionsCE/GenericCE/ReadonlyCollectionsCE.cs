using System;
using System.Collections.Generic;
using Composable.SystemCE.LinqCE;

namespace Composable.SystemCE.CollectionsCE.GenericCE
{
    static class ReadonlyCollectionsCE
    {
        public static Dictionary<TKey, TValue> AddToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, TKey key, TValue value) =>
            new Dictionary<TKey, TValue>(@this)
            {
                {key, value}
            };

        public static Dictionary<TKey, TValue> AddRangeToCopy<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> @this, IDictionary<TKey, TValue> range) =>
            new Dictionary<TKey, TValue>(@this)
               .Mutate(me => me.AddRange(range));


        public static List<T> AddToCopy<T>(this IReadOnlyList<T> @this, T item) =>
            new List<T>(@this)
            {
                item
            };

        public static List<T> AddRangeToCopy<T>(this IReadOnlyList<T> @this, IEnumerable<T> items) =>
            new List<T>(@this).Mutate(me => me.AddRange(items));

        public static T[] AddToCopy<T>(this T[] @this, T itemToAdd)
        {
            var copy = new T[@this.Length + 1];
            Array.Copy(@this, copy, @this.Length);
            copy[^1] = itemToAdd;
            return copy;
        }
    }
}
