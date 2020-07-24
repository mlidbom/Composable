using System.Collections.Generic;
using Composable.SystemCE.CollectionsCE.GenericCE;

namespace Composable.SystemCE.ThreadingCE
{
    static class ThreadSafe
    {
        internal static void AddToCopyAndReplace<T>(ref T[] original, T item) =>
            original = original.AddToCopy(item);

        internal static void AddToCopyAndReplace<T>(ref IReadOnlyList<T> original, T item) =>
            original = original.AddToCopy(item);

        internal static void AddRangeToCopyAndReplace<T>(ref IReadOnlyList<T> original, IEnumerable<T> item) =>
            original = original.AddRangeToCopy(item);

        internal static void AddToCopyAndReplace<TKey, TValue>(ref IReadOnlyDictionary<TKey, TValue> original, TKey key, TValue value) =>
            original = original.AddToCopy(key, value);

        internal static void AddRangeToCopyAndReplace<TKey, TValue>(ref IReadOnlyDictionary<TKey, TValue> original, IEnumerable<KeyValuePair<TKey, TValue>> additions) =>
            original = original.AddRangeToCopy(additions);
    }
}
