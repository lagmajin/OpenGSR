using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace UnityMCP.Editor.Handlers
{
    internal static class UnityObjectIdCompat
    {
        public static int GetObjectId(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            try
            {
                var getEntityId = obj.GetType().GetMethod("GetEntityId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (getEntityId != null)
                {
                    var entityId = getEntityId.Invoke(obj, null);
                    if (entityId != null)
                    {
                        if (int.TryParse(entityId.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        {
                            return parsed;
                        }

                        if (entityId is IConvertible convertible)
                        {
                            return convertible.ToInt32(CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                var legacy = obj.GetType().GetMethod("GetInstanceID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (legacy != null)
                {
                    var value = legacy.Invoke(obj, null);
                    if (value != null)
                    {
                        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        public static UnityEngine.Object ResolveEntityIdObject(int entityId)
        {
            try
            {
                foreach (var method in typeof(EditorUtility).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!string.Equals(method.Name, nameof(EditorUtility.EntityIdToObject), StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var parameters = method.GetParameters();
                    if (parameters.Length != 1)
                    {
                        continue;
                    }

                    var entityIdArg = CreateEntityId(parameters[0].ParameterType, entityId);
                    if (entityIdArg == null)
                    {
                        continue;
                    }

                    return method.Invoke(null, new[] { entityIdArg }) as UnityEngine.Object;
                }
            }
            catch
            {
            }

            return null;
        }

        private static object CreateEntityId(Type entityIdType, int value)
        {
            if (entityIdType == null)
            {
                return null;
            }

            try
            {
                var intCtor = entityIdType.GetConstructor(new[] { typeof(int) });
                if (intCtor != null)
                {
                    return intCtor.Invoke(new object[] { value });
                }

                var longCtor = entityIdType.GetConstructor(new[] { typeof(long) });
                if (longCtor != null)
                {
                    return longCtor.Invoke(new object[] { (long)value });
                }

                var parse = entityIdType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (parse != null)
                {
                    return parse.Invoke(null, new object[] { value.ToString() });
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
