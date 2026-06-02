using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OpenGS;

namespace OpenGS.EditorTools
{
    public static class WeaponSelectionSpriteAssigner
    {
        private const string WeaponDataFolder = "Assets/Resources/MasterData/Weapon";
        private const string WeaponSelectSpriteFolder = "Assets/Sprites/WeaponSelect";

        [MenuItem("OpenGSR/Tools/Assign Weapon Selection Sprites")]
        public static void AssignWeaponSelectionSprites()
        {
            var spriteLookup = BuildSpriteLookup();
            var assetGuids = AssetDatabase.FindAssets("t:WeaponMasterData", new[] { WeaponDataFolder });

            var updatedCount = 0;
            var skippedCount = 0;

            foreach (var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileName(path);
                if (string.Equals(fileName, "WeaponListMasterData.asset", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(fileName, "WeaponThumbnail.asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<WeaponMasterData>(path);
                if (asset == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(asset);
                var selectionSprite = serializedObject.FindProperty("inSelectionSprite");
                if (selectionSprite == null || selectionSprite.propertyType != SerializedPropertyType.ObjectReference)
                {
                    skippedCount++;
                    continue;
                }

                if (!TryResolveSprite(asset.name, spriteLookup, out var resolvedSprite))
                {
                    skippedCount++;
                    continue;
                }

                if (selectionSprite.objectReferenceValue == resolvedSprite)
                {
                    continue;
                }

                selectionSprite.objectReferenceValue = resolvedSprite;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                updatedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Assigned {updatedCount} weapon selection sprites. Skipped {skippedCount} assets without a match.");
        }

        private static Dictionary<string, Sprite> BuildSpriteLookup()
        {
            var lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            AddSpritesFromFolder(lookup, WeaponSelectSpriteFolder);
            return lookup;
        }

        private static void AddSpritesFromFolder(Dictionary<string, Sprite> lookup, string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                AddSpriteKey(lookup, Path.GetFileNameWithoutExtension(path), sprite);
                AddSpriteKey(lookup, sprite.name, sprite);
                AddSpriteKey(lookup, StripKnownPrefixes(Path.GetFileNameWithoutExtension(path)), sprite);
            }
        }

        private static void AddSpriteKey(Dictionary<string, Sprite> lookup, string key, Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(key) || sprite == null)
            {
                return;
            }

            var normalized = Normalize(key);
            if (!lookup.ContainsKey(normalized))
            {
                lookup[normalized] = sprite;
            }
        }

        private static bool TryResolveSprite(string weaponName, Dictionary<string, Sprite> lookup, out Sprite sprite)
        {
            foreach (var candidate in GetCandidates(weaponName))
            {
                var normalized = Normalize(candidate);
                if (lookup.TryGetValue(normalized, out sprite) && sprite != null)
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        private static IEnumerable<string> GetCandidates(string weaponName)
        {
            yield return weaponName;
            yield return StripKnownPrefixes(weaponName);

            switch (weaponName)
            {
                case "AK47":
                    yield return "UI_W_ak47";
                    break;
                case "AWP":
                    yield return "AWP";
                    break;
                case "Dragunov":
                    yield return "Dragunov";
                    break;
                case "F2000":
                    yield return "F2000";
                    break;
                case "FAMAS":
                    yield return "FAMAS";
                    break;
                case "M16":
                    yield return "M16A1";
                    yield return "M16A2";
                    break;
                case "MP5":
                    yield return "MP5";
                    break;
                case "PSG1":
                    yield return "PSG-1";
                    break;
                case "Scout":
                    yield return "Scout";
                    break;
                case "Scorpion":
                    yield return "Skorpion";
                    break;
                case "Uzi":
                    yield return "IMIUzi";
                    break;
                case "MG42":
                    yield return "MG42";
                    break;
                case "M60":
                    yield return "M60E4";
                    break;
                case "FNMinimiSaw":
                    yield return "FNMinimiSAW";
                    break;
                case "LaserGun":
                    yield return "LaserGun";
                    break;
                case "BubbleGun":
                    yield return "Bubble";
                    break;
                case "ChristmasGun":
                    yield return "xmas";
                    break;
                case "SteyrAug":
                    yield return "SteyrAUG";
                    break;
                case "Glock":
                    yield return "Glock18c";
                    break;
                case "DesertEagle":
                    yield return "DesertEagle";
                    break;
                case "FnP90":
                    yield return "P-90";
                    break;
            }
        }

        private static string StripKnownPrefixes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.StartsWith("SWeapon_B_", StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring("SWeapon_B_".Length);
            }

            if (value.StartsWith("UI_W_", StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring("UI_W_".Length);
            }

            return value;
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
    }
}
