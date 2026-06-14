using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenGS.EditorTools
{
    public sealed class SpriteReferenceRepairWindow : EditorWindow
    {
        private const string ReportDir = "Assets/Reports";
        private const string ReportPath = "Assets/Reports/SpriteReferenceRepairReport.md";

        [MenuItem("OpenGSR/Tools/Sprite Reference Repair")]
        private static void Open()
        {
            var window = GetWindow<SpriteReferenceRepairWindow>();
            window.titleContent = new GUIContent("Sprite Repair");
            window.minSize = new Vector2(620, 320);
            window.Show();
        }

        private Vector2 _scroll;
        private string _lastSummary = "Not scanned yet.";
        private string _lastReportPath;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprite Reference Repair", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans SpriteRenderer and UI Image components for broken sprite links, then suggests or applies best-match repairs from the sprite library.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Only", GUILayout.Height(26)))
            {
                RunRepair(autoRepair: false);
            }

            if (GUILayout.Button("Auto Repair High Confidence", GUILayout.Height(26)))
            {
                RunRepair(autoRepair: true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Output", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_lastSummary, GUILayout.Height(160));
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_lastReportPath))
            {
                EditorGUILayout.LabelField("Markdown", _lastReportPath);
            }
        }

        private void RunRepair(bool autoRepair)
        {
            if (!Directory.Exists(ReportDir))
            {
                Directory.CreateDirectory(ReportDir);
            }

            var candidates = BuildSpriteCandidates();
            var scenes = ScanScenes(candidates, autoRepair, out var openedScenes);
            try
            {
                var prefabs = ScanPrefabs(candidates, autoRepair);
                var totalBroken = scenes.Sum(s => s.BrokenEntries.Count) + prefabs.Sum(p => p.BrokenEntries.Count);
                var repaired = scenes.Sum(s => s.RepairedCount) + prefabs.Sum(p => p.RepairedCount);

                WriteReport(scenes, prefabs, totalBroken, repaired, autoRepair);

                _lastSummary =
                    $"Scenes scanned: {scenes.Count}\n" +
                    $"Prefabs scanned: {prefabs.Count}\n" +
                    $"Broken refs found: {totalBroken}\n" +
                    $"Repaired: {repaired}\n" +
                    $"Mode: {(autoRepair ? "auto repair high confidence" : "scan only")}\n" +
                    $"Markdown: {ReportPath}";
                _lastReportPath = ReportPath;
            }
            finally
            {
                for (var i = openedScenes.Count - 1; i >= 0; i--)
                {
                    var scene = openedScenes[i];
                    if (scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
        }

        private static List<SpriteCandidate> BuildSpriteCandidates()
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets" });
            var candidates = new List<SpriteCandidate>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is not Sprite sprite)
                    {
                        continue;
                    }

                    candidates.Add(new SpriteCandidate
                    {
                        Sprite = sprite,
                        AssetPath = path,
                        SpriteName = sprite.name,
                        NormalizedSpriteName = Normalize(sprite.name),
                        NormalizedAssetStem = Normalize(Path.GetFileNameWithoutExtension(path)),
                        NormalizedAssetPath = Normalize(path)
                    });
                }
            }

            return candidates;
        }

        private static List<AssetRepairReport> ScanScenes(List<SpriteCandidate> candidates, bool autoRepair, out List<Scene> openedScenes)
        {
            openedScenes = new List<Scene>();
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            var scenePaths = sceneGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path)
                .ToList();

            var reports = new List<AssetRepairReport>();
            foreach (var scenePath in scenePaths)
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (!scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    openedScenes.Add(scene);
                }

                reports.Add(ScanAsset(scene.name, scene.path, scene.GetRootGameObjects(), candidates, autoRepair, saveScene: scene));
            }

            return reports;
        }

        private static List<AssetRepairReport> ScanPrefabs(List<SpriteCandidate> candidates, bool autoRepair)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            var prefabPaths = prefabGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .OrderBy(path => path)
                .ToList();

            var reports = new List<AssetRepairReport>();
            foreach (var prefabPath in prefabPaths)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var report = ScanAsset(Path.GetFileNameWithoutExtension(prefabPath), prefabPath, new[] { root }, candidates, autoRepair, savePrefabPath: prefabPath);
                    reports.Add(report);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return reports;
        }

        private static AssetRepairReport ScanAsset(
            string assetName,
            string assetPath,
            GameObject[] roots,
            List<SpriteCandidate> candidates,
            bool autoRepair,
            Scene? saveScene = null,
            string savePrefabPath = null)
        {
            var report = new AssetRepairReport
            {
                AssetName = assetName,
                AssetPath = assetPath,
                AssetKind = savePrefabPath != null ? "Prefab" : "Scene"
            };

            foreach (var root in roots)
            {
                Walk(root.transform, assetPath, report, candidates, autoRepair, saveScene, savePrefabPath);
            }

            if (saveScene.HasValue && report.RepairedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(saveScene.Value);
                EditorSceneManager.SaveScene(saveScene.Value);
            }

            if (!string.IsNullOrEmpty(savePrefabPath) && report.RepairedCount > 0)
            {
                var root = roots[0];
                PrefabUtility.SaveAsPrefabAsset(root, savePrefabPath);
                AssetDatabase.SaveAssets();
            }

            return report;
        }

        private static void Walk(
            Transform transform,
            string assetPath,
            AssetRepairReport report,
            List<SpriteCandidate> candidates,
            bool autoRepair,
            Scene? saveScene,
            string savePrefabPath)
        {
            InspectComponent(transform.GetComponent<SpriteRenderer>(), transform, report, candidates, autoRepair, saveScene, savePrefabPath);
            InspectComponent(transform.GetComponent<Image>(), transform, report, candidates, autoRepair, saveScene, savePrefabPath);

            for (var i = 0; i < transform.childCount; i++)
            {
                Walk(transform.GetChild(i), assetPath, report, candidates, autoRepair, saveScene, savePrefabPath);
            }
        }

        private static void InspectComponent(
            Component component,
            Transform transform,
            AssetRepairReport report,
            List<SpriteCandidate> candidates,
            bool autoRepair,
            Scene? saveScene,
            string savePrefabPath)
        {
            if (component == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(component);
            var spriteProp = serializedObject.FindProperty("m_Sprite");
            if (spriteProp == null || spriteProp.propertyType != SerializedPropertyType.ObjectReference)
            {
                return;
            }

            if (spriteProp.objectReferenceValue != null)
            {
                return;
            }

            var instanceId = spriteProp.objectReferenceEntityIdValue.ToString();
            if (string.IsNullOrEmpty(instanceId) || instanceId == "0")
            {
                return;
            }

            var goPath = GetHierarchyPath(transform);
            var componentType = component.GetType().Name;
            var broken = new BrokenSpriteEntry
            {
                GameObjectPath = goPath,
                ComponentType = componentType,
                FieldName = "sprite",
                MissingInstanceId = instanceId
            };

            var candidate = FindBestCandidate(broken, candidates);
            if (candidate != null)
            {
                broken.CandidatePath = candidate.AssetPath;
                broken.CandidateSpriteName = candidate.SpriteName;
                broken.CandidateScore = candidate.Score;
            }

            report.BrokenEntries.Add(broken);

            if (!autoRepair || candidate == null || candidate.Score < 90)
            {
                return;
            }

            spriteProp.objectReferenceValue = candidate.Sprite;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(component);

            if (saveScene.HasValue)
            {
                EditorSceneManager.MarkSceneDirty(saveScene.Value);
            }

            report.RepairedCount++;
        }

        private static SpriteCandidateResult FindBestCandidate(BrokenSpriteEntry broken, List<SpriteCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var goName = Normalize(Path.GetFileName(broken.GameObjectPath));
            var pathName = Normalize(broken.GameObjectPath);
            var componentName = Normalize(broken.ComponentType);

            SpriteCandidateResult best = null;
            foreach (var candidate in candidates)
            {
                var score = ScoreCandidate(goName, pathName, componentName, candidate);
                if (score <= 0)
                {
                    continue;
                }

                if (best == null || score > best.Score)
                {
                    best = new SpriteCandidateResult
                    {
                        Sprite = candidate.Sprite,
                        AssetPath = candidate.AssetPath,
                        SpriteName = candidate.SpriteName,
                        Score = score
                    };
                }
            }

            return best;
        }

        private static int ScoreCandidate(string goName, string pathName, string componentName, SpriteCandidate candidate)
        {
            var score = 0;

            if (!string.IsNullOrEmpty(goName))
            {
                if (candidate.NormalizedSpriteName == goName) score += 120;
                if (candidate.NormalizedAssetStem == goName) score += 110;
                if (candidate.NormalizedSpriteName.Contains(goName)) score += 70;
                if (goName.Contains(candidate.NormalizedSpriteName)) score += 50;
                if (candidate.NormalizedAssetStem.Contains(goName)) score += 60;
                if (goName.Contains(candidate.NormalizedAssetStem)) score += 40;
                if (candidate.NormalizedAssetPath.Contains(goName)) score += 20;
                if (pathName.Contains(candidate.NormalizedSpriteName)) score += 15;
            }

            if (candidate.AssetPath.IndexOf("/Sprites/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            if (candidate.AssetPath.IndexOf("/Archive/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 10;
            }

            if (string.Equals(componentName, "image", StringComparison.Ordinal))
            {
                if (candidate.AssetPath.IndexOf("/UI/", StringComparison.OrdinalIgnoreCase) >= 0) score += 15;
                if (candidate.AssetPath.IndexOf("/ui", StringComparison.OrdinalIgnoreCase) >= 0) score += 10;
            }

            return score;
        }

        private static string GetHierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            var current = t;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static void WriteReport(IEnumerable<AssetRepairReport> scenes, IEnumerable<AssetRepairReport> prefabs, int totalBroken, int totalRepaired, bool autoRepair)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Sprite Reference Repair Report");
            sb.AppendLine();
            sb.AppendLine($"- Mode: {(autoRepair ? "Auto Repair High Confidence" : "Scan Only")}");
            sb.AppendLine($"- Scenes scanned: {scenes.Count()}");
            sb.AppendLine($"- Prefabs scanned: {prefabs.Count()}");
            sb.AppendLine($"- Broken sprite refs: {totalBroken}");
            sb.AppendLine($"- Repaired: {totalRepaired}");
            sb.AppendLine();
            sb.AppendLine("## Notes");
            sb.AppendLine("- Auto repair only applies when the best candidate is strong enough.");
            sb.AppendLine("- If a reference is not repaired, inspect the suggested candidate or restore the original asset.");
            sb.AppendLine();

            foreach (var asset in scenes.Concat(prefabs))
            {
                sb.AppendLine($"### {asset.AssetName}");
                sb.AppendLine($"- Path: `{asset.AssetPath}`");
                if (asset.BrokenEntries.Count == 0)
                {
                    sb.AppendLine("- Broken refs: none");
                    sb.AppendLine();
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine("| GameObject | Component | Missing Instance ID | Suggested Sprite | Candidate Score |");
                sb.AppendLine("| --- | --- | --- | --- | --- |");
                foreach (var entry in asset.BrokenEntries)
                {
                    sb.AppendLine($"| `{entry.GameObjectPath}` | `{entry.ComponentType}` | `{entry.MissingInstanceId}` | `{entry.CandidatePath ?? string.Empty}` | {entry.CandidateScore} |");
                }
                sb.AppendLine();
            }

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        [Serializable]
        private sealed class AssetRepairReport
        {
            public string AssetName;
            public string AssetPath;
            public string AssetKind;
            public List<BrokenSpriteEntry> BrokenEntries = new();
            public int RepairedCount;
        }

        [Serializable]
        private sealed class BrokenSpriteEntry
        {
            public string GameObjectPath;
            public string ComponentType;
            public string FieldName;
            public string MissingInstanceId;
            public string CandidatePath;
            public string CandidateSpriteName;
            public int CandidateScore;
        }

        private sealed class SpriteCandidate
        {
            public Sprite Sprite;
            public string AssetPath;
            public string SpriteName;
            public string NormalizedSpriteName;
            public string NormalizedAssetStem;
            public string NormalizedAssetPath;
        }

        private sealed class SpriteCandidateResult
        {
            public Sprite Sprite;
            public string AssetPath;
            public string SpriteName;
            public int Score;
        }
    }
}
