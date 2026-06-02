using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenGSCore;
using UnityEditor;
using UnityEngine;

namespace OpenGS.EditorTools
{
    public static class WeaponMasterDataRebuilder
    {
        private const string WeaponDataFolder = "Assets/Resources/MasterData/Weapon";
        private const string WeaponThumbnailPath = "Assets/Resources/MasterData/Weapon/WeaponThumbnail.asset";
        private static readonly string[] SelectionFolders = { "WeaponSelect" };
        private static readonly string[] GameFolders = { "Weapon/MiniWeapon", "Weapon", "Archive/Weapon", "WeaponSelect" };
        private static readonly string[] SilhouetteFolders = { "Archive/Weapon", "Weapon/MiniWeapon", "Weapon", "WeaponSelect" };

        [MenuItem("OpenGSR/Tools/Rebuild Weapon Master Data")]
        public static void RebuildWeaponMasterData()
        {
            var spriteIndex = BuildSpriteIndex();
            var weaponValues = Enum.GetValues(typeof(EWeaponType))
                .Cast<EWeaponType>()
                .Where(v => v != EWeaponType.None)
                .ToArray();

            var updatedCount = 0;
            var createdCount = 0;
            var missingSpriteCount = 0;

            foreach (var weapon in weaponValues)
            {
                var assetPath = Path.Combine(WeaponDataFolder, $"{weapon}.asset").Replace('\\', '/');
                var asset = AssetDatabase.LoadAssetAtPath<WeaponMasterData>(assetPath);
                if (asset == null)
                {
                    if (File.Exists(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    asset = ScriptableObject.CreateInstance<WeaponMasterData>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                    createdCount++;
                }

                var candidates = GetWeaponCandidates(weapon.ToString());
                var selectionSprite = ResolveSprite(candidates, SelectionFolders, spriteIndex);
                var inGameSprite = ResolveSprite(candidates, GameFolders, spriteIndex);
                var silhouetteSprite = ResolveSprite(candidates, SilhouetteFolders, spriteIndex);

                if (inGameSprite == null)
                {
                    inGameSprite = selectionSprite;
                }

                if (silhouetteSprite == null)
                {
                    silhouetteSprite = inGameSprite;
                }

                if (selectionSprite == null)
                {
                    selectionSprite = inGameSprite;
                }

                if (selectionSprite == null || inGameSprite == null || silhouetteSprite == null)
                {
                    missingSpriteCount++;
                    Debug.LogWarning(
                        $"[WeaponMasterDataRebuilder] Missing sprite(s) for {weapon}: " +
                        $"selection={(selectionSprite != null)}, inGame={(inGameSprite != null)}, silhouette={(silhouetteSprite != null)}");
                }

                asset.weaponType = weapon;
                asset.reloadTime = 2f;
                asset.maxBullet = asset.maxBullet <= 0 ? 30 : asset.maxBullet;
                asset.inSelectionSprite = selectionSprite;
                asset.inGameSprite = inGameSprite;
                asset.shilhouetteSprite = silhouetteSprite;

                EditorUtility.SetDirty(asset);
                updatedCount++;
            }

            RebuildWeaponThumbnail(spriteIndex);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[WeaponMasterDataRebuilder] Rebuilt {updatedCount} weapon master data assets. " +
                $"Created {createdCount}, missing-sprite entries {missingSpriteCount}.");
        }

        private static void RebuildWeaponThumbnail(Dictionary<string, List<SpriteRecord>> spriteIndex)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WeaponThumbnailPath);
            if (asset == null)
            {
                Debug.LogWarning($"[WeaponMasterDataRebuilder] WeaponThumbnail asset not found at {WeaponThumbnailPath}.");
                return;
            }

            var serializedObject = new SerializedObject(asset);
            var assignments = new (string fieldName, Sprite sprite)[]
            {
                ("noneThumbnail", ResolveSprite(new[] { "UI_W_None", "Weapon_Empty", "None" }, new[] { "Weapon", "Archive/Weapon", "WeaponSelect" }, spriteIndex)),
                ("ak47Thumbnail", ResolveSprite(new[] { "AK47", "UI_W_ak47", "Weapon_ak47" }, SelectionFolders, spriteIndex)),
                ("awpThumbnail", ResolveSprite(new[] { "AWP", "UI_W_AWP", "Weapon_AWP" }, SelectionFolders, spriteIndex)),
                ("m16Thumbnail", ResolveSprite(new[] { "M16", "M16A1", "M16A2", "UI_W_M16A1", "UI_W_M16A2" }, SelectionFolders, spriteIndex)),
                ("psg1Thumbnail", ResolveSprite(new[] { "PSG1", "PSG-1", "UI_W_PSG-1", "UI_W_PSG1" }, SelectionFolders, spriteIndex)),
                ("bubbleGunThumbnail", ResolveSprite(new[] { "BubbleGun", "Bubble", "UI_W_Bubble_Gun", "UI_W_Bubble" }, SelectionFolders, spriteIndex)),
                ("f2000Thumbnail", ResolveSprite(new[] { "F2000", "UI_W_F2000", "Weapon_F2000" }, SelectionFolders, spriteIndex)),
                ("chirstmasGunThumbnail", ResolveSprite(new[] { "ChristmasGun", "ChirstmasGun", "xmas", "UI_W_xmas", "UI_W_XMas" }, SelectionFolders, spriteIndex)),
                ("fnp90Thumbnail", ResolveSprite(new[] { "FnP90", "FNP90", "FN_P90", "P-90", "P90", "UI_W_P-90" }, SelectionFolders, spriteIndex)),
            };

            var updated = 0;
            foreach (var (fieldName, sprite) in assignments)
            {
                var property = serializedObject.FindProperty(fieldName);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (property.objectReferenceValue == sprite)
                {
                    continue;
                }

                property.objectReferenceValue = sprite;
                updated++;
            }

            if (updated > 0)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                Debug.Log($"[WeaponMasterDataRebuilder] Updated {updated} fields on WeaponThumbnail.");
            }
        }

        private static Dictionary<string, List<SpriteRecord>> BuildSpriteIndex()
        {
            var index = new Dictionary<string, List<SpriteRecord>>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in new[] { "Assets/Sprites/WeaponSelect", "Assets/Sprites/Weapon", "Assets/Sprites/Archive/Weapon" })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                    {
                        continue;
                    }

                    var record = new SpriteRecord(sprite, path);
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    AddRecord(index, fileName, record);
                    AddRecord(index, StripKnownPrefixes(fileName), record);
                    AddRecord(index, StripKnownPrefixes(StripKnownPrefixes(fileName)), record);
                }
            }

            return index;
        }

        private static void AddRecord(Dictionary<string, List<SpriteRecord>> index, string key, SpriteRecord record)
        {
            var normalized = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (!index.TryGetValue(normalized, out var list))
            {
                list = new List<SpriteRecord>();
                index[normalized] = list;
            }

            if (!list.Any(existing => existing.Path == record.Path))
            {
                list.Add(record);
            }
        }

        private static Sprite ResolveSprite(IEnumerable<string> candidates, IEnumerable<string> folderPriority, Dictionary<string, List<SpriteRecord>> index)
        {
            foreach (var candidate in candidates)
            {
                var key = NormalizeKey(candidate);
                if (string.IsNullOrWhiteSpace(key) || !index.TryGetValue(key, out var records))
                {
                    continue;
                }

                foreach (var folder in folderPriority)
                {
                    var match = records.FirstOrDefault(r => PathContainsFolder(r.Path, folder));
                    if (match.Sprite != null)
                    {
                        return match.Sprite;
                    }
                }

                var first = records.FirstOrDefault(r => r.Sprite != null);
                if (first.Sprite != null)
                {
                    return first.Sprite;
                }
            }

            return null;
        }

        private static bool PathContainsFolder(string assetPath, string folder)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || string.IsNullOrWhiteSpace(folder))
            {
                return false;
            }

            var normalizedPath = assetPath.Replace('\\', '/').ToLowerInvariant();
            var normalizedFolder = folder.Replace('\\', '/').ToLowerInvariant();
            return normalizedPath.Contains($"/{normalizedFolder}/") ||
                   normalizedPath.EndsWith($"/{normalizedFolder}") ||
                   normalizedPath.StartsWith($"{normalizedFolder}/");
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        private static string StripKnownPrefixes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var result = value;
            foreach (var prefix in new[] { "SWeapon_B_", "SWeapon_S_", "UI_W_", "Weapon_", "arch_weapon_weapon_", "arch_weapon_", "weapon_" })
            {
                if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(prefix.Length);
                }
            }

            return result;
        }

        private static IEnumerable<string> GetWeaponCandidates(string weapon)
        {
            switch (weapon)
            {
                case "AK47": return new[] { "AK47", "ak47", "UI_W_ak47", "Weapon_ak47", "UI_W_AK47" };
                case "M16": return new[] { "M16", "M16A1", "M16A2", "UI_W_M16A1", "UI_W_M16A2", "Weapon_M16", "Weapon_M16A1", "Weapon_M16A2" };
                case "FAMAS": return new[] { "FAMAS", "UI_W_FAMAS", "Weapon_FAMAS" };
                case "F2000": return new[] { "F2000", "UI_W_F2000", "Weapon_F2000", "Weapon_F2001" };
                case "Scorpion": return new[] { "Scorpion", "Skorpion", "UI_W_Skorpion", "Weapon_Skorpion", "Weapon_Scorpion" };
                case "FnP90": return new[] { "FnP90", "FNP90", "FN_P90", "P-90", "P90", "FM_P90", "UI_W_P-90", "UI_W_FNP90", "Weapon_P-90", "Weapon_FM_P90" };
                case "Scout": return new[] { "Scout", "UI_W_Scout", "Weapon_Scout" };
                case "Dragunov": return new[] { "Dragunov", "UI_W_Dragunov", "UI_W_DRAGUNOV", "Weapon_Dragunov" };
                case "PSG1": return new[] { "PSG1", "PSG-1", "PSG_1", "UI_W_PSG-1", "UI_W_PSG1", "Weapon_PSG-1" };
                case "AWP": return new[] { "AWP", "UI_W_AWP", "Weapon_AWP" };
                case "Uzi": return new[] { "Uzi", "IMIUzi", "IMI_Uzi", "UI_W_IMIUzi", "Weapon_IMIUzi" };
                case "MG42": return new[] { "MG42", "UI_W_MG42", "Weapon_MG42" };
                case "M60": return new[] { "M60", "M60E4", "UI_W_M60E4", "Weapon_M60E4" };
                case "FNMinimiSaw": return new[] { "FNMinimiSaw", "FNMinimi_SAW", "FNMinimiSAW", "FnMINIMI_SAW", "UI_W_FNMinimiSAW", "UI_W_FnMINIMI_SAW", "Weapon_FNMinimiSAW" };
                case "LaserGun": return new[] { "LaserGun", "UI_W_LaserGun", "Weapon_LaserGun" };
                case "BubbleGun": return new[] { "BubbleGun", "Bubble_Gun", "Bubble", "UI_W_Bubble_Gun", "UI_W_Bubble", "Weapon_Bubble_Gun", "Weapon_Bubble" };
                case "ChristmasGun": return new[] { "ChristmasGun", "ChirstmasGun", "xmas", "Xmas", "UI_W_xmas", "Weapon_xmas" };
                case "SteyrAug": return new[] { "SteyrAug", "SteyrAUG", "SteyAug", "UI_W_SteyrAUG", "Weapon_SteyrAUG" };
                case "Glock": return new[] { "Glock", "Glock18c", "GLOCK18c", "UI_W_Glock18c", "UI_W_GLOCK18c", "Weapon_Glock18c" };
                case "DesertEagle": return new[] { "DesertEagle", "DESERTEAGLE", "DE", "UI_W_DESERTEAGLE", "Weapon_DESERTEAGLE" };
                case "MP5": return new[] { "MP5", "UI_W_MP5", "weapon_mp5", "Weapon_MP5" };
                default: return new[] { weapon };
            }
        }

        private sealed class SpriteRecord
        {
            public SpriteRecord(Sprite sprite, string path)
            {
                Sprite = sprite;
                Path = path;
            }

            public Sprite Sprite { get; }
            public string Path { get; }
        }
    }
}
