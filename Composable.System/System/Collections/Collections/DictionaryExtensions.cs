#region usings

using System;
using System.Collections.Generic;

using Composable.Contracts;

#endregion

namespace Composable.System.Collections.Collections
{
    ///<summary>Helpers for working with dictionaries</summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// If <paramref name="key"/> exists in me <paramref name="me"/> it is returned.
        /// If not <paramref name="constructor"/> is used to create a new value that is inserted into <paramref name="me"/> and returned.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key, Func<TValue> constructor)
        {
            Contract.Argument(() => me, () => key, () => constructor).NotNull();

            TValue value;
            if(me.TryGetValue(key, out value))
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
            Contract.Argument(() => me, () => key).NotNull();
            //Originally written to delegate to the above method. Belive it or not this causes a performancedecrease that is actually significant in tight loops.
            TValue value;
            if (me.TryGetValue(key, out value))
            {
                return value;
            }

            value = new TValue();
            me.Add(key, value);
            return value;
        }
    }
}