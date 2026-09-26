#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace OpenGS.Editor
{
    /// <summary>
    /// Lightweight checks for scene names used by master data and Build Settings.
    /// </summary>
    public static class OpenGSReferenceValidation
    {
        private static readonly Regex SceneNamePattern =
            new Regex(@"m_SceneName:\s*(\S+)", RegexOptions.Compiled);

        [MenuItem("OpenGS/Validation/Validate Scene References")]
        public static void ValidateSceneReferences()
        {
            var buildSceneNames = new HashSet<string>();
            var missingBuildFiles = 0;

            foreach (var scene in EditorBuildSettings.scenes)
            {
                var projectPath = scene.path.Replace('\\', '/');
                if (!File.Exists(projectPath))
                {
                    Debug.LogError($"[OpenGSValidation] Build Settings scene is missing: {projectPath}");
                    missingBuildFiles++;
                    continue;
                }

                buildSceneNames.Add(Path.GetFileNameWithoutExtension(projectPath));
            }

            var missingMasterScenes = 0;
            var masterGuids = AssetDatabase.FindAssets("", new[] { "Assets/Resources/MasterData" });
            foreach (var guid in masterGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".asset"))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                foreach (Match match in SceneNamePattern.Matches(text))
                {
                    var sceneName = match.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(sceneName) && !buildSceneNames.Contains(sceneName))
                    {
                        Debug.LogWarning($"[OpenGSValidation] Scene '{sceneName}' from {path} is not in Build Settings.");
                        missingMasterScenes++;
                    }
                }
            }

            Debug.Log($"[OpenGSValidation] Completed. Missing build files: {missingBuildFiles}, " +
                      $"master-data scene warnings: {missingMasterScenes}.");
        }

        [MenuItem("OpenGS/Validation/Validate Weapon Prefab References")]
        public static void ValidateWeaponPrefabReferences()
        {
            var weaponPaths = new Dictionary<string, string>
            {
                { "AK47", "Prefabs/Weapon/Guns/AR/AK47" },
                { "M16", "Prefabs/Weapon/Guns/AR/M16" },
                { "FAMAS", "Prefabs/Weapon/Guns/AR/FAMAS" },
                { "F2000", "Prefabs/Weapon/Guns/AR/F2000" },
                { "SteyrAug", "Prefabs/Weapon/Guns/AR/SteyrAug" },
                { "Scorpion", "Prefabs/Weapon/Guns/SMG/Scorpion" },
                { "FnP90", "Prefabs/Weapon/Guns/SMG/P90" },
                { "Uzi", "Prefabs/Weapon/Guns/SMG/Uzi" },
                { "MP5", "Prefabs/Weapon/Guns/SMG/MP5" },
                { "Scout", "Prefabs/Weapon/Guns/Sniper/Scout" },
                { "Dragunov", "Prefabs/Weapon/Guns/Sniper/Dragunov" },
                { "PSG1", "Prefabs/Weapon/Guns/Sniper/PSG1" },
                { "AWP", "Prefabs/Weapon/Guns/Sniper/AWP" },
                { "MG42", "Prefabs/Weapon/Guns/MG/MG42" },
                { "M60", "Prefabs/Weapon/Guns/MG/M60E4" },
                { "FNMinimiSaw", "Prefabs/Weapon/Guns/MG/FNMinimiSAW" },
                { "Glock", "Prefabs/Weapon/Guns/Pistol/Glock" },
                { "DesertEagle", "Prefabs/Weapon/Guns/Pistol/DesertEagle" },
                { "LaserGun", "Prefabs/Weapon/Guns/Special/LaserGun" },
                { "BubbleGun", "Prefabs/Weapon/Guns/Special/BubbleGun" }
            };

            var missingInResources = 0;
            var missingInProject = 0;
            foreach (var pair in weaponPaths)
            {
                var resourcePath = $"Assets/Resources/{pair.Value}.prefab";
                var projectPath = $"Assets/{pair.Value}.prefab";
                var inResources = File.Exists(resourcePath);
                var inProject = File.Exists(projectPath);

                if (!inResources)
                {
                    Debug.LogWarning($"[OpenGSValidation] Weapon '{pair.Key}' is not under Resources: {pair.Value}");
                    missingInResources++;
                }

                if (!inProject && !inResources)
                {
                    Debug.LogError($"[OpenGSValidation] Weapon '{pair.Key}' prefab is missing: {pair.Value}");
                    missingInProject++;
                }
            }

            Debug.Log($"[OpenGSValidation] Weapon validation completed. " +
                      $"Missing Resources entries: {missingInResources}, missing prefabs: {missingInProject}.");
        }

        [MenuItem("OpenGS/Validation/Validate Missing Scripts")]
        public static void ValidateMissingScripts()
        {
            var knownScriptGuids = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                knownScriptGuids.Add(guid);
            }

            var unresolvedReferences = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".unity") && !path.EndsWith(".prefab"))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                foreach (Match match in Regex.Matches(
                    text,
                    @"m_Script:\s*\{fileID: 11500000, guid: ([0-9a-f]+), type: 3\}"))
                {
                    if (!knownScriptGuids.Contains(match.Groups[1].Value))
                    {
                        unresolvedReferences.Add($"{match.Groups[1].Value}|{path}");
                    }
                }
            }

            foreach (var reference in unresolvedReferences)
            {
                var separator = reference.IndexOf('|');
                var guid = reference.Substring(0, separator);
                var path = reference.Substring(separator + 1);
                Debug.LogWarning($"[OpenGSValidation] Unresolved script reference (confirm in Unity Inspector): {guid} in {path}");
            }

            Debug.Log($"[OpenGSValidation] Missing-script validation completed. " +
                      $"Unresolved references: {unresolvedReferences.Count}.");
        }
    }
}
#endif
