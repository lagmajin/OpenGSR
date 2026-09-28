#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGSCore
{
    public static class JObjectExtensions
    {
        public static bool ContainsAllKeys(this JObject jObject, params string[] keys)
        {
            return keys.All(key =>
            {
                string jsonPath = key.Replace("/", "."); // `/` を `.` に統一
                return jObject.SelectToken(jsonPath) != null;
            });
        }

        public static string? GetStringOrNull(this JObject obj, string key)
        {
            return obj.TryGetValue(key, out JToken? token) && token != null ? token.ToString() : null;
        }

        public static string? GetStringAny(this JObject obj, params string[] keys)
        {
            if (obj == null || keys == null)
            {
                return null;
            }

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (obj.TryGetValue(key, out JToken? token) && token != null)
                {
                    var value = token.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        public static int? GetIntAny(this JObject obj, params string[] keys)
        {
            if (obj == null || keys == null)
            {
                return null;
            }

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!obj.TryGetValue(key, out JToken? token) || token == null)
                {
                    continue;
                }

                try
                {
                    return token.ToObject<int>();
                }
                catch
                {
                }
            }

            return null;
        }

        public static bool? GetBoolAny(this JObject obj, params string[] keys)
        {
            if (obj == null || keys == null)
            {
                return null;
            }

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!obj.TryGetValue(key, out JToken? token) || token == null)
                {
                    continue;
                }

                try
                {
                    return token.ToObject<bool>();
                }
                catch
                {
                }
            }

            return null;
        }
    }

}
