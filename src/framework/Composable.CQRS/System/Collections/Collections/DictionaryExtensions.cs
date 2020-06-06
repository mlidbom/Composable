using System;
using System.Collections.Generic;

using Composable.Contracts;

namespace Composable.System.Collections.Collections
{
    ///<summary>Helpers for working with dictionaries</summary>
    static class DictionaryExtensions
    {
        /// <summary>
        /// If <paramref name="key"/> exists in me <paramref name="me"/> it is returned.
        /// If not <paramref name="constructor"/> is used to create a new value that is inserted into <paramref name="me"/> and returned.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key, Func<TValue> constructor)
        {
            Contract.ArgumentNotNull(me, nameof(me), key, nameof(key), constructor, nameof(constructor));

            if (me.TryGetValue(key, out var value))
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
            Contract.ArgumentNotNull(me, nameof(me), key, nameof(key));
            //Originally written to delegate to the above method. Believe it or not this causes a performance decrease that is actually significant in tight loops.
            if (me.TryGetValue(key, out var value))
            {
                return value;
            }

            value = new TValue();
            me.Add(key, value);
            return value;
        }


        public static TValue GetAndRemove<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key)
        {
            var value = @this[key];
            @this.Remove(key);
            return value;
        }
    }
}