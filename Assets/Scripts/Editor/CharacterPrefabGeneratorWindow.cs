using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenGSCore;
using UnityEditor;
using UnityEngine;

namespace OpenGS
{
    public class CharacterPrefabGeneratorWindow : EditorWindow
    {
        [SerializeField] private GameObject sourcePrefab;
        [SerializeField] private string outputFolder = "Assets/Resources/Prefabs/Players";
        [SerializeField] private PlayerPrefabMasterData masterData;
        [SerializeField] private string masterDataPath = "Assets/Resources/MasterData/Player/PlayerPrefabMasterData.asset";
        [SerializeField] private bool autoApplyCharacterSprites = true;
        [SerializeField] private bool includeAmi = true;
        [SerializeField] private bool includeYumi = true;
        [SerializeField] private bool includeJack = true;
        [SerializeField] private bool includeJackle = true;
        [SerializeField] private bool includeMisty = true;
        [SerializeField] private bool includeLiu = true;
        [SerializeField] private bool includeMary = true;
        [SerializeField] private bool includeWolf = true;
        [SerializeField] private bool includeWyvern = true;
        [SerializeField] private bool includeSeoul = true;
        [SerializeField] private bool includeLittleJ = true;
        [SerializeField] private bool includeShue = true;
        [SerializeField] private bool includeSwaltz = true;

        [MenuItem("Tools/Player/Generate Character Prefabs")]
        public static void ShowWindow()
        {
            GetWindow<CharacterPrefabGeneratorWindow>("Character Prefab Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Template Prefab", sourcePrefab, typeof(GameObject), false);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            masterData = (PlayerPrefabMasterData)EditorGUILayout.ObjectField("Master Data", masterData, typeof(PlayerPrefabMasterData), false);
            masterDataPath = EditorGUILayout.TextField("Master Data Path", masterDataPath);
            autoApplyCharacterSprites = EditorGUILayout.ToggleLeft("Auto Apply Character Sprites", autoApplyCharacterSprites);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

            includeAmi = EditorGUILayout.ToggleLeft("Ami", includeAmi);
            includeYumi = EditorGUILayout.ToggleLeft("Yumi", includeYumi);
            includeJack = EditorGUILayout.ToggleLeft("Jack", includeJack);
            includeJackle = EditorGUILayout.ToggleLeft("Jackle", includeJackle);
            includeMisty = EditorGUILayout.ToggleLeft("Misty", includeMisty);
            includeLiu = EditorGUILayout.ToggleLeft("Liu", includeLiu);
            includeMary = EditorGUILayout.ToggleLeft("Mary", includeMary);
            includeWolf = EditorGUILayout.ToggleLeft("Wolf", includeWolf);
            includeWyvern = EditorGUILayout.ToggleLeft("Wyvern", includeWyvern);
            includeSeoul = EditorGUILayout.ToggleLeft("Seoul", includeSeoul);
            includeLittleJ = EditorGUILayout.ToggleLeft("LittleJ", includeLittleJ);
            includeShue = EditorGUILayout.ToggleLeft("Shue", includeShue);
            includeSwaltz = EditorGUILayout.ToggleLeft("Swaltz", includeSwaltz);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(sourcePrefab == null))
            {
                if (GUILayout.Button("Generate Prefabs"))
                {
                    GeneratePrefabsAndRegister();
                }
            }

            if (GUILayout.Button("Ping Output Folder"))
            {
                PingOutputFolder();
            }
        }

        private void GeneratePrefabsAndRegister()
        {
            if (sourcePrefab == null)
            {
                Debug.LogWarning("[CharacterPrefabGeneratorWindow] Source prefab is not assigned.");
                return;
            }

            if (!AssetDatabase.Contains(sourcePrefab))
            {
                Debug.LogWarning("[CharacterPrefabGeneratorWindow] Source prefab must be an asset prefab.");
                return;
            }

            EnsureFolder(outputFolder);
            EnsureMasterData();

            var characters = BuildCharacterList();
            var createdPaths = new List<string>();

            foreach (var character in characters)
            {
                var outputPath = Path.Combine(outputFolder, $"{character}.prefab").Replace("\\", "/");
                if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
                {
                    if (!EditorUtility.DisplayDialog(
                            "Prefab Exists",
                            $"{outputPath} already exists. Overwrite it?",
                            "Overwrite",
                            "Skip"))
                    {
                        continue;
                    }
                }

                var result = PrefabUtility.SaveAsPrefabAsset(sourcePrefab, outputPath);
                if (result != null)
                {
                    var report = new List<string>();

                    if (autoApplyCharacterSprites)
                    {
                        ApplyCharacterSprites(result, character, report);
                    }

                    ApplyCharacterTag(result, character);
                    createdPaths.Add(outputPath);
                    RegisterToMasterData(character, result);

                    if (report.Count > 0)
                    {
                        Debug.Log($"[CharacterPrefabGeneratorWindow] {character} sprite mapping:\n- {string.Join("\n- ", report)}");
                    }
                }
            }

            if (masterData != null)
            {
                EditorUtility.SetDirty(masterData);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CharacterPrefabGeneratorWindow] Generated {createdPaths.Count} prefabs:\n{string.Join("\n", createdPaths)}");
        }

        private List<EPlayerCharacter> BuildCharacterList()
        {
            var result = new List<EPlayerCharacter>();
            if (includeAmi) result.Add(EPlayerCharacter.Ami);
            if (includeYumi) result.Add(EPlayerCharacter.Yumi);
            if (includeJack) result.Add(EPlayerCharacter.Jack);
            if (includeJackle) result.Add(EPlayerCharacter.Jackle);
            if (includeMisty) result.Add(EPlayerCharacter.Misty);
            if (includeLiu) result.Add(EPlayerCharacter.Liu);
            if (includeMary) result.Add(EPlayerCharacter.Mary);
            if (includeWolf) result.Add(EPlayerCharacter.Wolf);
            if (includeWyvern) result.Add(EPlayerCharacter.Wyvern);
            if (includeSeoul) result.Add(EPlayerCharacter.Seoul);
            if (includeLittleJ) result.Add(EPlayerCharacter.LittleJ);
            if (includeShue) result.Add(EPlayerCharacter.Shue);
            if (includeSwaltz) result.Add(EPlayerCharacter.Swaltz);
            return result;
        }

        private void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var normalized = folderPath.Replace("\\", "/").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("Output folder is empty.");
            }

            var parts = normalized.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private void PingOutputFolder()
        {
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputFolder);
            if (folder != null)
            {
                EditorGUIUtility.PingObject(folder);
            }
            else
            {
                Debug.LogWarning($"[CharacterPrefabGeneratorWindow] Folder not found: {outputFolder}");
            }
        }

        private void EnsureMasterData()
        {
            if (masterData != null)
            {
                return;
            }

            masterData = AssetDatabase.LoadAssetAtPath<PlayerPrefabMasterData>(masterDataPath);
            if (masterData == null)
            {
                Debug.LogWarning($"[CharacterPrefabGeneratorWindow] Master data not found: {masterDataPath}");
            }
        }

        private static void ApplyCharacterTag(GameObject prefab, EPlayerCharacter character)
        {
            if (prefab == null)
            {
                return;
            }

            var player = prefab.GetComponent<AbstractPlayer>();
            if (player != null)
            {
                var field = typeof(AbstractPlayer).GetField("character", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                field?.SetValue(player, character);
            }
        }

        private static void ApplyCharacterSprites(GameObject prefab, EPlayerCharacter character, List<string> report = null)
        {
            if (prefab == null)
            {
                return;
            }

            var alias = GetCharacterAlias(character);
            var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var partName = renderer.gameObject.name;
                var sprite = ResolveCharacterSprite(alias, partName);
                if (sprite != null)
                {
                    renderer.sprite = sprite;
                    report?.Add($"{partName} -> {sprite.name}");
                }
            }
        }

        private static Sprite ResolveCharacterSprite(string alias, string partName)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return null;
            }

            var normalizedPart = NormalizePartName(partName);
            var candidates = new List<string>();

            if (normalizedPart.Contains("death"))
            {
                candidates.Add($"Sprites/Player/{alias}/{alias}Death");
                candidates.Add($"Sprites/Player/{alias}/{alias}Death2");
            }

            if (normalizedPart.Contains("head"))
            {
                candidates.Add($"Sprites/Player/{alias}/{alias}Head");
                candidates.Add($"Sprites/Player/{alias}/P7_Head");
                candidates.Add($"Sprites/Player/{alias}/P11_Head");
            }

            if (normalizedPart.Contains("body"))
            {
                candidates.Add($"Sprites/Player/{alias}/{alias}Body");
                candidates.Add($"Sprites/Player/{alias}/P7_Body");
                candidates.Add($"Sprites/Player/{alias}/p11_body");
            }

            if (normalizedPart.Contains("arm1") || normalizedPart.Contains("leftarm") || normalizedPart.Contains("armleft"))
            {
                candidates.Add($"Sprites/Player/{alias}/{alias}Arm");
                candidates.Add($"Sprites/Player/{alias}/P11_Arm1");
                candidates.Add($"Sprites/Player/{alias}/AmiArm");
            }

            if (normalizedPart.Contains("arm2") || normalizedPart.Contains("rightarm") || normalizedPart.Contains("armright"))
            {
                candidates.Add($"Sprites/Player/{alias}/{alias}Arm2");
                candidates.Add($"Sprites/Player/{alias}/P11_Arm2");
                candidates.Add($"Sprites/Player/{alias}/AmiArm2");
            }

            candidates.Add($"Sprites/Player/{alias}/{alias}");
            candidates.Add($"Sprites/Player/{alias}/{alias}Lei");
            candidates.Add($"Sprites/Player/{alias}/{alias}Death");

            foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static string GetCharacterAlias(EPlayerCharacter character)
        {
            return character switch
            {
                EPlayerCharacter.Ami => "Ami",
                EPlayerCharacter.Yumi => "Yumi",
                EPlayerCharacter.Jack => "Jack",
                EPlayerCharacter.Jackle => "Jackal",
                EPlayerCharacter.Misty => "Misty",
                EPlayerCharacter.Liu => "Liu",
                EPlayerCharacter.Mary => "Mary",
                EPlayerCharacter.Wolf => "Wolf",
                EPlayerCharacter.Wyvern => "Wyvern",
                EPlayerCharacter.Seoul => "Seoul",
                EPlayerCharacter.LittleJ => "LittleJ",
                EPlayerCharacter.Shue => "Shue",
                EPlayerCharacter.Swaltz => "Schwartz",
                _ => character.ToString()
            };
        }

        private static string NormalizePartName(string partName)
        {
            return string.IsNullOrWhiteSpace(partName)
                ? string.Empty
                : partName.ToLowerInvariant().Replace(" ", "").Replace("_", "");
        }

        private void RegisterToMasterData(EPlayerCharacter character, GameObject prefab)
        {
            if (masterData == null || prefab == null)
            {
                return;
            }

            if (character == EPlayerCharacter.Misty)
            {
                masterData.mistyPrefab = prefab;
                return;
            }

            var listField = typeof(PlayerPrefabMasterData).GetField("characterPrefabs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (listField == null)
            {
                return;
            }

            var list = listField.GetValue(masterData) as IList<PlayerPrefabMasterData.CharacterPrefabEntry>;
            if (list == null)
            {
                return;
            }

            var existing = list.FirstOrDefault(entry => entry != null && entry.character == character);
            if (existing != null)
            {
                existing.prefab = prefab;
            }
            else
            {
                list.Add(new PlayerPrefabMasterData.CharacterPrefabEntry
                {
                    character = character,
                    prefab = prefab
                });
            }
        }
    }
}
