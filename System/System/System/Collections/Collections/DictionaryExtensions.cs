#region usings

using System;
using System.Collections.Generic;
using Composable.System.Collections.Collections;

#endregion

namespace Composable.System.Collections.Collections
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// If <paramref name="key"/> exists in me <paramref name="me"/> it is returned.
        /// If not <paramref name="constructor"/> is used to create a new value that is inserted into <paramref name="me"/> and returned. 
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key, Func<TValue> constructor)
        {
            TValue value;
            if (me.TryGetValue(key, out value))
            {
                return value;
            }

            value = constructor();
            me.Add(key, value);
            return value;
        }

        /// <summary>
        /// If <paramref name="key"/> exists in me <paramref name="me"/> it is returned if not it is inserted from the default constructor and returned.
        /// </summary>
        public static TValue GetOrAddDefault<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key) where TValue : new()
        {
            return me.GetOrAdd(key, () => new TValue());
        }
    }
}