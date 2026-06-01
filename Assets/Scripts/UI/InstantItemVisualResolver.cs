using System;
using System.Collections.Generic;
using System.Linq;
using OpenGSCore;
using UnityEngine;

namespace OpenGS
{
    public static class InstantItemVisualResolver
    {
        private static readonly Dictionary<EInstantItemType, Sprite> iconCache = new Dictionary<EInstantItemType, Sprite>();
        private static readonly Dictionary<string, Sprite> resourceSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static InstantItemThumbnailMasterData masterData;

        public static string GetDisplayName(EInstantItemType type)
        {
            return type switch
            {
                EInstantItemType.HealthKit => "Health Kit",
                EInstantItemType.FireBullet => "Fire Bullet",
                EInstantItemType.PoisonBullet => "Poison Bullet",
                EInstantItemType.PowerGrenadePack => "Power Grenade Pack",
                EInstantItemType.ClusterGrenadePack => "Cluster Grenade Pack",
                EInstantItemType.MagnetGrenadePack => "Magnet Grenade Pack",
                EInstantItemType.MineGrenadePack => "Mine Grenade Pack",
                _ => type.ToString()
            };
        }

        public static string GetEffectName(EInstantItemType type)
        {
            return type switch
            {
                EInstantItemType.HealthKit => "heal",
                EInstantItemType.FireBullet => "fire_bullet",
                EInstantItemType.PoisonBullet => "poison_bullet",
                EInstantItemType.PowerGrenadePack => "power_grenade_pack",
                EInstantItemType.ClusterGrenadePack => "cluster_grenade_pack",
                EInstantItemType.MagnetGrenadePack => "magnet_grenade_pack",
                EInstantItemType.MineGrenadePack => "mine_grenade_pack",
                _ => "unknown"
            };
        }

        public static Sprite GetIcon(EInstantItemType type)
        {
            if (iconCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            Sprite sprite = null;
            var data = GetMasterData();
            if (data != null)
            {
                sprite = GetMasterDataSprite(data, type);
            }

            if (sprite == null)
            {
                sprite = GetFallbackSprite(type);
            }

            if (sprite != null)
            {
                iconCache[type] = sprite;
            }

            return sprite;
        }

        public static void ClearCache()
        {
            iconCache.Clear();
            resourceSpriteCache.Clear();
            masterData = null;
        }

        private static InstantItemThumbnailMasterData GetMasterData()
        {
            if (masterData != null)
            {
                return masterData;
            }

            var candidates = Resources.LoadAll<InstantItemThumbnailMasterData>("MasterData/Item");
            masterData = candidates.FirstOrDefault(asset =>
                asset != null && asset.name.IndexOf("Thumbnail", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? candidates.FirstOrDefault(asset => asset != null);

            return masterData;
        }

        private static Sprite GetMasterDataSprite(InstantItemThumbnailMasterData data, EInstantItemType type)
        {
            if (data.thumbnail != null && data.thumbnail.TryGetValue(type, out var thumbnail) && thumbnail != null)
            {
                return thumbnail;
            }

            return type switch
            {
                EInstantItemType.HealthKit => data.normalGrenade != null ? data.normalGrenade : data.thumbnail?.Values.FirstOrDefault(sprite => sprite != null),
                EInstantItemType.FireBullet => data.fireGrenade,
                EInstantItemType.PoisonBullet => data.smokeGrenade,
                EInstantItemType.PowerGrenadePack => data.powerGrenade,
                EInstantItemType.ClusterGrenadePack => data.clusterGrenade,
                EInstantItemType.MagnetGrenadePack => data.magneticGrenade,
                EInstantItemType.MineGrenadePack => data.mineGrenade,
                _ => null
            };
        }

        private static Sprite GetFallbackSprite(EInstantItemType type)
        {
            return type switch
            {
                EInstantItemType.HealthKit => LoadSprite("Sprites/Item/InstantItem/Slot_Band", "Sprites/Item/item_AidKit"),
                EInstantItemType.FireBullet => LoadSprite("Sprites/Item/InstantItem/Slot_Firebullets", "Sprites/Item/item_Fire"),
                EInstantItemType.PoisonBullet => LoadSprite("Sprites/Item/InstantItem/Slot_PoisonBullets", "Sprites/Item/item_Effect"),
                EInstantItemType.PowerGrenadePack => LoadSprite("Sprites/Item/InstantItem/Slot_Powergrenade", "Sprites/Item/item_Power"),
                EInstantItemType.ClusterGrenadePack => LoadSprite("Sprites/Item/InstantItem/Slot_Clustergranade", "Sprites/Item/item_Grenade"),
                EInstantItemType.MagnetGrenadePack => LoadSprite("Sprites/Item/InstantItem/Slot_MagneticGrenade_S", "Sprites/Item/item_Grenade 1"),
                EInstantItemType.MineGrenadePack => LoadSprite("Sprites/Item/InstantItem/Slot_Landmine", "Sprites/Item/item_Launcher"),
                _ => null
            };
        }

        private static Sprite LoadSprite(params string[] resourcePaths)
        {
            foreach (var path in resourcePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (resourceSpriteCache.TryGetValue(path, out var cached))
                {
                    if (cached != null)
                    {
                        return cached;
                    }

                    continue;
                }

                var sprite = Resources.Load<Sprite>(path);
                resourceSpriteCache[path] = sprite;
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
