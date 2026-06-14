using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace OpenGS
{
    public static class UnityObjectIdCompat
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
    }
}
