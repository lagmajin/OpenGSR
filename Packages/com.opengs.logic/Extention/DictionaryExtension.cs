using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public static  class DictionaryExtension
    {
        public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> dic, TK key, TV defaultValue = default) => dic.TryGetValue(key, out var result) ? result : defaultValue;

        public static TV AddOrUpdate<TK, TV>(this IDictionary<TK, TV> dic, TK key, TV value)
        {
            if (dic == null)
            {
                return default;
            }

            dic[key] = value;
            return value;
        }

        public static bool TryAdd<TK, TV>(this IDictionary<TK, TV> dic, TK key, TV value)
        {
            if (dic == null || dic.ContainsKey(key))
            {
                return false;
            }

            dic.Add(key, value);
            return true;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, Func<TKey, TValue> factory)
        {
            if (dic == null)
            {
                return default;
            }

            if (dic.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var created = factory != null ? factory(key) : default;
            dic[key] = created;
            return created;
        }
    }
}
