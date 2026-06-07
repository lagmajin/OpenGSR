using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenGS
{
    /// <summary>
    /// Weapon enum -> master data -> sprites/display name の共通入口。
    /// UI ごとの個別ハードコードを減らすため、ここを参照元にする。
    /// </summary>
    public static class WeaponVisualResolver
    {
        private static readonly Dictionary<string, WeaponMasterData> weaponDataCache = new Dictionary<string, WeaponMasterData>(StringComparer.OrdinalIgnoreCase);
        private static bool initialized;

        public static WeaponMasterData Resolve(Enum weaponType)
        {
            if (weaponType == null)
            {
                return null;
            }

            EnsureInitialized();

            foreach (var key in GetCandidateKeys(weaponType.ToString()))
            {
                if (TryGetCachedData(key, out var data))
                {
                    return data;
                }
            }

            return null;
        }

        public static WeaponMasterData Resolve(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return null;
            }

            EnsureInitialized();

            foreach (var key in GetCandidateKeys(weaponId))
            {
                if (TryGetCachedData(key, out var data))
                {
                    return data;
                }
            }

            return null;
        }

        public static Sprite GetSelectionSprite(Enum weaponType)
        {
            var data = Resolve(weaponType);
            return data?.inSelectionSprite;
        }

        public static Sprite GetSelectionSprite(string weaponId)
        {
            var data = Resolve(weaponId);
            return data?.inSelectionSprite;
        }

        public static Sprite GetInGameSprite(Enum weaponType)
        {
            var data = Resolve(weaponType);
            return data != null
                ? data.inGameSprite ?? data.inSelectionSprite ?? data.shilhouetteSprite
                : null;
        }

        public static Sprite GetInGameSprite(string weaponId)
        {
            var data = Resolve(weaponId);
            return data != null
                ? data.inGameSprite ?? data.inSelectionSprite ?? data.shilhouetteSprite
                : null;
        }

        public static Sprite GetSilhouetteSprite(Enum weaponType)
        {
            var data = Resolve(weaponType);
            return data != null
                ? data.shilhouetteSprite ?? data.inSelectionSprite ?? data.inGameSprite
                : null;
        }

        public static Sprite GetSilhouetteSprite(string weaponId)
        {
            var data = Resolve(weaponId);
            return data != null
                ? data.shilhouetteSprite ?? data.inSelectionSprite ?? data.inGameSprite
                : null;
        }

        public static string GetDisplayName(Enum weaponType)
        {
            return GetDisplayName(weaponType?.ToString());
        }

        public static string GetDisplayName(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return string.Empty;
            }

            var data = Resolve(weaponId);
            if (data != null)
            {
                return NormalizeDisplayName(data.weaponType.ToString());
            }

            return NormalizeDisplayName(weaponId);
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

                Register(asset.weaponType.ToString(), asset);
                Register(asset.name, asset);
                Register(Normalize(asset.name), asset);

                foreach (var alias in GetCandidateKeys(asset.weaponType.ToString()))
                {
                    Register(alias, asset);
                }
            }
        }

        private static void Register(string key, WeaponMasterData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var normalized = Normalize(key);
            if (!weaponDataCache.ContainsKey(normalized))
            {
                weaponDataCache[normalized] = data;
            }
        }

        private static bool TryGetCachedData(string key, out WeaponMasterData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return weaponDataCache.TryGetValue(Normalize(key), out data) && data != null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToUpperInvariant();
        }

        private static string NormalizeDisplayName(string weaponId)
        {
            switch (weaponId)
            {
                case "FnP90":
                case "FN_P90":
                case "P-90":
                    return "FN_P90";
                case "FNMinimiSaw":
                case "FNMinimi_SAW":
                    return "FNMinimi_SAW";
                case "SteyrAug":
                case "SteyAug":
                case "SteyrAUG":
                    return "SteyrAug";
                case "ChristmasGun":
                case "ChirstmasGun":
                    return "ChristmasGun";
                case "DesertEagle":
                case "DE":
                    return "DE";
                case "Scorpion":
                case "Skorpion":
                    return "Scorpion";
                case "Glock":
                case "Glock18c":
                    return "Glock";
                case "Shotgun":
                    return "Spas";
                case "M60":
                case "M60E4":
                    return "M60";
                case "M16":
                case "M16A1":
                case "M16A2":
                case "M4":
                case "M4A1":
                    return "M16";
                case "Uzi":
                case "IMIUzi":
                    return "Uzi";
                case "PSG":
                case "PSG-1":
                    return "PSG1";
                default:
                    return weaponId;
            }
        }

        private static IEnumerable<string> GetCandidateKeys(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                yield break;
            }

            yield return weaponId;
            yield return Normalize(weaponId);

            switch (weaponId)
            {
                case "DesertEagle":
                case "DE":
                    yield return "DE";
                    break;
                case "FnP90":
                case "FN_P90":
                case "P-90":
                    yield return "FN_P90";
                    yield return "P-90";
                    break;
                case "FNMinimiSaw":
                case "FNMinimi_SAW":
                    yield return "FNMinimi_SAW";
                    break;
                case "SteyrAug":
                case "SteyAug":
                case "SteyrAUG":
                    yield return "SteyrAUG";
                    yield return "SteyAug";
                    break;
                case "ChristmasGun":
                case "ChirstmasGun":
                    yield return "ChirstmasGun";
                    break;
                case "Scorpion":
                case "Skorpion":
                    yield return "Skorpion";
                    break;
                case "Glock":
                case "Glock18c":
                    yield return "Glock18c";
                    break;
                case "Shotgun":
                    yield return "Spas";
                    break;
                case "M60":
                case "M60E4":
                    yield return "M60E4";
                    break;
                case "M16":
                case "M16A1":
                case "M16A2":
                case "M4":
                case "M4A1":
                    yield return "M16A1";
                    yield return "M16A2";
                    break;
                case "Uzi":
                case "IMIUzi":
                    yield return "IMIUzi";
                    break;
                case "PSG":
                case "PSG-1":
                    yield return "PSG1";
                    break;
                case "AK47":
                case "UI_W_ak47":
                    yield return "UI_W_ak47";
                    break;
                case "BubbleGun":
                case "Bubble":
                    yield return "Bubble";
                    break;
            }
        }
    }
}
