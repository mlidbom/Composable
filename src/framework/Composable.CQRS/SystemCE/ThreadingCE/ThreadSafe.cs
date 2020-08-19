using System.Collections.Generic;
using Composable.SystemCE.CollectionsCE.GenericCE;

namespace Composable.SystemCE.ThreadingCE
{
    static class ThreadSafe
    {
        ///<summary>Must be called from synchronized code that guarantees that this is the only thread modifying <paramref name="original"/>. It's purpose is to allow readers free access to <paramref name="original"/> even though <paramref name="original"/> is not thread safe.</summary>
        internal static void AddToCopyAndReplace<T>(ref T[] original, T item) =>
            original = original.AddToCopy(item);

        ///<summary>Must be called from synchronized code that guarantees that this is the only thread modifying <paramref name="original"/>. It's purpose is to allow readers free access to <paramref name="original"/> even though <paramref name="original"/> is not thread safe.</summary>
        internal static void AddToCopyAndReplace<T>(ref IReadOnlyList<T> original, T item) =>
            original = original.AddToCopy(item);

        ///<summary>Must be called from synchronized code that guarantees that this is the only thread modifying <paramref name="original"/>. It's purpose is to allow readers free access to <paramref name="original"/> even though <paramref name="original"/> is not thread safe.</summary>
        internal static void AddRangeToCopyAndReplace<T>(ref IReadOnlyList<T> original, IEnumerable<T> item) =>
            original = original.AddRangeToCopy(item);

        ///<summary>Must be called from synchronized code that guarantees that this is the only thread modifying <paramref name="original"/>. It's purpose is to allow readers free access to <paramref name="original"/> even though <paramref name="original"/> is not thread safe.</summary>
        internal static void AddToCopyAndReplace<TKey, TValue>(ref IReadOnlyDictionary<TKey, TValue> original, TKey key, TValue value) where TKey : notnull =>
            original = original.AddToCopy(key, value);

        ///<summary>Must be called from synchronized code that guarantees that this is the only thread modifying <paramref name="original"/>. It's purpose is to allow readers free access to <paramref name="original"/> even though <paramref name="original"/> is not thread safe.</summary>
        internal static void AddRangeToCopyAndReplace<TKey, TValue>(ref IReadOnlyDictionary<TKey, TValue> original, IEnumerable<KeyValuePair<TKey, TValue>> additions) where TKey : notnull =>
            original = original.AddRangeToCopy(additions);
    }
}
