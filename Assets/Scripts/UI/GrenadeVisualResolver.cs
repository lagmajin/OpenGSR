using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Grenade enum -> HUD sprite / projectile prefab / effect prefab の共通入口。
    /// 既存の master data をまたいで、呼び出し側は enum だけを見る。
    /// </summary>
    public static class GrenadeVisualResolver
    {
        private static readonly Dictionary<EGrenadeType, ScriptableObject> legacyDataCache = new Dictionary<EGrenadeType, ScriptableObject>();
        private static readonly Dictionary<string, Sprite> hudSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static bool initialized;

        public static string GetDisplayName(EGrenadeType type)
        {
            return type switch
            {
                EGrenadeType.Normal => "Normal",
                EGrenadeType.Power => "Power",
                EGrenadeType.Magnetic => "Magnetic",
                EGrenadeType.Mine => "Mine",
                EGrenadeType.Cluster => "Cluster",
                EGrenadeType.ClusterChild => "ClusterChild",
                EGrenadeType.Fire => "Fire",
                EGrenadeType.Smoke => "Smoke",
                _ => type.ToString()
            };
        }

        public static string GetInternalName(EGrenadeType type)
        {
            return type switch
            {
                EGrenadeType.Normal => "Grenade",
                EGrenadeType.Power => "PowerGrenade",
                EGrenadeType.Magnetic => "MagneticGrenade",
                EGrenadeType.Mine => "MineGrenade",
                EGrenadeType.Cluster => "ClusterGrenade",
                EGrenadeType.ClusterChild => "ChildClusterGrenade",
                EGrenadeType.Fire => "FireGrenade",
                EGrenadeType.Smoke => "SmokeGrenade",
                _ => type.ToString()
            };
        }

        public static Sprite GetHudSprite(EGrenadeType type)
        {
            EnsureInitialized();

            var legacySprite = GetLegacySprite(type);
            if (legacySprite != null)
            {
                return legacySprite;
            }

            var key = Normalize(type.ToString());
            if (hudSpriteCache.TryGetValue(key, out var sprite) && sprite != null)
            {
                return sprite;
            }

            return null;
        }

        public static GameObject GetProjectilePrefab(EGrenadeType type, AllGrenadeListMasterData listMasterData = null)
        {
            if (listMasterData != null && listMasterData.dataList != null)
            {
                foreach (var entry in listMasterData.dataList)
                {
                    if (entry == null || entry.GrenadePrefab == null || string.IsNullOrWhiteSpace(entry.Name))
                    {
                        continue;
                    }

                    if (string.Equals(entry.Name, type.ToString(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.Name, $"{type}Grenade", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.Name, $"{type}Bomb", StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.GrenadePrefab;
                    }
                }
            }

            var legacyPrefab = GetLegacyPrefab(type);
            if (legacyPrefab != null)
            {
                return legacyPrefab;
            }

            if (type == EGrenadeType.Smoke)
            {
                return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/SmokeGrenade");
            }

            return null;
        }

        public static GameObject GetExplosionEffect(EGrenadeType type)
        {
            if (type == EGrenadeType.Smoke)
            {
                return Resources.Load<GameObject>("Prefabs/Weapon/Projectile/SmokeBombEffect");
            }

            return null;
        }

        public static Sprite GetPackHudSprite(EGrenadeType type)
        {
            return GetHudSprite(type);
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            Register("Normal", "Sprites/Grenade/Grenade_Normal");
            Register("Power", "Sprites/Grenade/Grenade_Power");
            Register("Magnetic", "Sprites/Grenade/Grenade_Magnetic");
            Register("Mine", "Sprites/Grenade/Grenade_Mine");
            Register("Cluster", "Sprites/Grenade/Grenade_Cluster");
            Register("Fire", "Sprites/Grenade/Grenade_Fire");
            Register("Smoke", "Sprites/Grenade/Grenade_Smoke");
        }

        private static ScriptableObject GetLegacyData(EGrenadeType type)
        {
            if (legacyDataCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var resourceName = type switch
            {
                EGrenadeType.Normal => "MasterData/Grenade/NormalGrenade",
                EGrenadeType.Power => "MasterData/Grenade/PowerGrenade",
                EGrenadeType.Fire => "MasterData/Grenade/FireGrenade",
                EGrenadeType.Mine => "MasterData/Grenade/MineGrenade",
                EGrenadeType.Magnetic => "MasterData/Grenade/MagneticGrenade",
                EGrenadeType.Cluster => "MasterData/Grenade/ClusterGrenade",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            cached = Resources.Load<ScriptableObject>(resourceName);
            legacyDataCache[type] = cached;
            return cached;
        }

        private static Sprite GetLegacySprite(EGrenadeType type)
        {
            var data = GetLegacyData(type);
            if (data == null)
            {
                return null;
            }

            return GetObjectMember<Sprite>(data, "slotImage");
        }

        private static GameObject GetLegacyPrefab(EGrenadeType type)
        {
            var data = GetLegacyData(type);
            if (data == null)
            {
                return null;
            }

            return GetObjectMember<GameObject>(data, "GrenadePrefab");
        }

        private static T GetObjectMember<T>(UnityEngine.Object source, string memberName) where T : UnityEngine.Object
        {
            if (source == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = source.GetType();

            var field = type.GetField(memberName, flags);
            if (field != null && typeof(T).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(source) as T;
            }

            var property = type.GetProperty(memberName, flags);
            if (property != null && typeof(T).IsAssignableFrom(property.PropertyType))
            {
                return property.GetValue(source) as T;
            }

            return null;
        }

        private static void Register(string key, string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                return;
            }

            hudSpriteCache[Normalize(key)] = sprite;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }
    }
}
