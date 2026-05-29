using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Weapon master assets under Resources/MasterData/Weapon are treated as the source of truth
    /// for selection icons. This avoids manually wiring sprites per UI slot.
    /// </summary>
    public static class WeaponSelectionSpriteResolver
    {
        private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static bool initialized;

        public static Sprite Resolve(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            EnsureInitialized();

            foreach (var alias in GetAliases(weaponId))
            {
                if (TryGetCachedSprite(alias, out var sprite))
                {
                    return sprite;
                }
            }

            return null;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            var assets = Resources.LoadAll<WeaponMasterData>("MasterData/Weapon");
            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }

                var sprite = asset.inSelectionSprite
                    ?? asset.inGameSprite
                    ?? asset.shilhouetteSprite;
                if (sprite == null)
                {
                    continue;
                }

                AddCacheKey(asset.name, sprite);
                AddCacheKey(Normalize(asset.name), sprite);

                foreach (var alias in GetAliases(asset.name))
                {
                    AddCacheKey(alias, sprite);
                }
            }
        }

        private static bool TryGetCachedSprite(string key, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return spriteCache.TryGetValue(Normalize(key), out sprite) && sprite != null;
        }

        private static void AddCacheKey(string key, Sprite sprite)
        {
            if (sprite == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var normalized = Normalize(key);
            if (spriteCache.ContainsKey(normalized))
            {
                return;
            }

            spriteCache[normalized] = sprite;
        }

        private static string Normalize(string weaponId)
        {
            return weaponId
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static IEnumerable<string> GetAliases(string weaponId)
        {
            yield return weaponId;
            yield return Normalize(weaponId);

            switch (weaponId)
            {
                case "DesertEagle":
                    yield return "DE";
                    break;
                case "FnP90":
                    yield return "FN_P90";
                    break;
                case "FNMinimiSaw":
                    yield return "FNMinimi_SAW";
                    break;
                case "SteyrAug":
                    yield return "SteyAug";
                    break;
                case "ChristmasGun":
                    yield return "ChirstmasGun";
                    break;
            }
        }
    }
}
