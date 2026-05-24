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
    public sealed class SpriteSceneUsageReport : EditorWindow
    {
        private const string ReportDir = "Assets/Reports";
        private const string ReportPath = "Assets/Reports/SpriteSceneUsageReport.md";
        private const string SnapshotPath = "Assets/Reports/SpriteSceneUsageReport.json";

        [MenuItem("OpenGSR/Tools/Sprite Scene Usage Report")]
        private static void Open()
        {
            var window = GetWindow<SpriteSceneUsageReport>();
            window.titleContent = new GUIContent("Sprite Usage");
            window.minSize = new Vector2(520, 240);
            window.Show();
        }

        private Vector2 _scroll;
        private string _lastSummary = "Not generated yet.";
        private string _lastReportPath;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprite Scene Usage Report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans scenes and prefab assets, then records which SpriteRenderer/Image components reference which sprite assets. "
                + "This is meant to make sprite recovery easier when references go missing.",
                MessageType.Info);

            if (GUILayout.Button("Generate Report"))
            {
                Generate();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Output", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_lastSummary, GUILayout.Height(120));
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_lastReportPath))
            {
                EditorGUILayout.LabelField("Markdown", _lastReportPath);
                EditorGUILayout.LabelField("Snapshot", SnapshotPath);
            }
        }

        private static bool IsSpritePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith("Assets/Sprites/", StringComparison.OrdinalIgnoreCase)) return false;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd";
        }

        private void Generate()
        {
            if (!Directory.Exists(ReportDir))
            {
                Directory.CreateDirectory(ReportDir);
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            var scenePaths = sceneGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => p)
                .ToList();

            var scenes = new List<AssetReport>();
            var prefabs = new List<AssetReport>();
            var openedScenes = new List<Scene>();

            try
            {
                foreach (var scenePath in scenePaths)
                {
                    var scene = SceneManager.GetSceneByPath(scenePath);
                    if (!scene.isLoaded)
                    {
                        scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                        openedScenes.Add(scene);
                    }
                    scenes.Add(CollectScene(scene));
                }
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

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            var prefabPaths = prefabGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => p)
                .ToList();

            foreach (var prefabPath in prefabPaths)
            {
                prefabs.Add(CollectPrefab(prefabPath));
            }

            var totalRefs = scenes.Sum(s => s.Entries.Count) + prefabs.Sum(p => p.Entries.Count);
            var totalBrokenRefs = scenes.Sum(s => s.BrokenEntries.Count) + prefabs.Sum(p => p.BrokenEntries.Count);
            var totalSprites = scenes
                .Concat(prefabs)
                .SelectMany(s => s.Entries.Select(e => e.SpritePath))
                .Distinct()
                .Count();

            WriteMarkdown(scenes, prefabs, totalRefs, totalBrokenRefs, totalSprites);
            WriteSnapshot(scenes, prefabs, totalRefs, totalBrokenRefs, totalSprites);

            _lastSummary =
                $"Scenes: {scenes.Count}\n" +
                $"Prefabs: {prefabs.Count}\n" +
                $"Scene sprite refs: {totalRefs}\n" +
                $"Broken sprite refs: {totalBrokenRefs}\n" +
                $"Unique sprites: {totalSprites}\n" +
                $"Markdown: {ReportPath}\n" +
                $"Snapshot: {SnapshotPath}";
            _lastReportPath = ReportPath;
        }

        private static AssetReport CollectScene(Scene scene)
        {
            var report = new AssetReport
            {
                AssetName = scene.name,
                AssetPath = scene.path,
                AssetKind = "Scene",
            };

            foreach (var root in scene.GetRootGameObjects())
            {
                CollectGameObject(root.transform, report);
            }

            report.Entries = report.Entries
                .OrderBy(e => e.GameObjectPath)
                .ThenBy(e => e.ComponentType)
                .ThenBy(e => e.FieldName)
                .ToList();

            return report;
        }

        private static AssetReport CollectPrefab(string prefabPath)
        {
            var report = new AssetReport
            {
                AssetName = Path.GetFileNameWithoutExtension(prefabPath),
                AssetPath = prefabPath,
                AssetKind = "Prefab",
            };

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CollectGameObject(prefabRoot.transform, report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            report.Entries = report.Entries
                .OrderBy(e => e.GameObjectPath)
                .ThenBy(e => e.ComponentType)
                .ThenBy(e => e.FieldName)
                .ToList();

            return report;
        }

        private static void CollectGameObject(Transform root, AssetReport report)
        {
            var goPath = GetHierarchyPath(root);

            var spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                CollectSpriteReference(spriteRenderer, goPath, nameof(SpriteRenderer), report);
            }

            var image = root.GetComponent<Image>();
            if (image != null)
            {
                CollectSpriteReference(image, goPath, nameof(Image), report);
            }

            for (var i = 0; i < root.childCount; i++)
            {
                CollectGameObject(root.GetChild(i), report);
            }
        }

        private static void CollectSpriteReference(Component component, string goPath, string componentType, AssetReport report)
        {
            var serializedObject = new SerializedObject(component);
            var spriteProp = serializedObject.FindProperty("m_Sprite");
            if (spriteProp == null || spriteProp.propertyType != SerializedPropertyType.ObjectReference)
            {
                return;
            }

            var sprite = spriteProp.objectReferenceValue as Sprite;
            if (sprite != null)
            {
                AddEntry(report, goPath, componentType, "sprite", sprite);
                return;
            }

            var instanceId = spriteProp.objectReferenceInstanceIDValue;
            if (instanceId != 0)
            {
                report.BrokenEntries.Add(new BrokenSpriteEntry
                {
                    GameObjectPath = goPath,
                    ComponentType = componentType,
                    FieldName = "sprite",
                    InstanceId = instanceId,
                });
            }
        }

        private static void AddEntry(AssetReport report, string goPath, string componentType, string fieldName, Sprite sprite)
        {
            var spritePath = AssetDatabase.GetAssetPath(sprite);
            if (!IsSpritePath(spritePath))
            {
                return;
            }

            report.Entries.Add(new SpriteEntry
            {
                GameObjectPath = goPath,
                ComponentType = componentType,
                FieldName = fieldName,
                SpriteName = sprite.name,
                SpritePath = spritePath,
                Guid = AssetDatabase.AssetPathToGUID(spritePath),
            });
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

        private static void WriteMarkdown(IEnumerable<AssetReport> scenes, IEnumerable<AssetReport> prefabs, int totalRefs, int totalBrokenRefs, int totalSprites)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Sprite Scene And Prefab Usage Report");
            sb.AppendLine();
            sb.AppendLine("This report records which scene objects and prefab contents reference sprites.");
            sb.AppendLine("It is intended as a recovery aid when links are moved or lost.");
            sb.AppendLine();
            sb.AppendLine($"- Scenes scanned: {scenes.Count()}");
            sb.AppendLine($"- Prefabs scanned: {prefabs.Count()}");
            sb.AppendLine($"- Sprite refs found: {totalRefs}");
            sb.AppendLine($"- Broken sprite refs found: {totalBrokenRefs}");
            sb.AppendLine($"- Unique sprites: {totalSprites}");
            sb.AppendLine();
            sb.AppendLine("## How to use this");
            sb.AppendLine("- Search by scene or prefab name to find the object path that owned a sprite.");
            sb.AppendLine("- If a link breaks, restore the asset and reassign the sprite at the listed GameObject path.");
            sb.AppendLine("- Broken references are listed separately with the GameObject path and component type.");
            sb.AppendLine("- This tracks SpriteRenderer and UI Image references in both scenes and prefab assets.");
            sb.AppendLine();

            sb.AppendLine("## Scene Assets");
            sb.AppendLine();
            foreach (var scene in scenes)
            {
                WriteAssetSection(sb, scene);
            }

            sb.AppendLine("## Prefab Assets");
            sb.AppendLine();
            foreach (var prefab in prefabs)
            {
                WriteAssetSection(sb, prefab);
            }

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static void WriteAssetSection(StringBuilder sb, AssetReport asset)
        {
            sb.AppendLine($"### {asset.AssetName}");
            sb.AppendLine($"- Path: `{asset.AssetPath}`");

            if (asset.Entries.Count == 0)
            {
                sb.AppendLine("- Sprite refs: none found");
            }

            if (asset.Entries.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("| GameObject | Component | Field | Sprite | Asset Path | GUID |");
                sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
                foreach (var entry in asset.Entries)
                {
                    sb.AppendLine($"| `{entry.GameObjectPath}` | `{entry.ComponentType}` | `{entry.FieldName}` | `{entry.SpriteName}` | `{entry.SpritePath}` | `{entry.Guid}` |");
                }
            }

            if (asset.BrokenEntries.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("| GameObject | Component | Field | Missing Instance ID |");
                sb.AppendLine("| --- | --- | --- | --- |");
                foreach (var entry in asset.BrokenEntries)
                {
                    sb.AppendLine($"| `{entry.GameObjectPath}` | `{entry.ComponentType}` | `{entry.FieldName}` | `{entry.InstanceId}` |");
                }
            }

            sb.AppendLine();
        }

        private static void WriteSnapshot(IEnumerable<AssetReport> scenes, IEnumerable<AssetReport> prefabs, int totalRefs, int totalBrokenRefs, int totalSprites)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"generated_at\": \"{EscapeJson(DateTime.UtcNow.ToString("o"))}\",");
            sb.AppendLine($"  \"scene_count\": {scenes.Count()},");
            sb.AppendLine($"  \"prefab_count\": {prefabs.Count()},");
            sb.AppendLine($"  \"sprite_ref_count\": {totalRefs},");
            sb.AppendLine($"  \"broken_sprite_ref_count\": {totalBrokenRefs},");
            sb.AppendLine($"  \"unique_sprite_count\": {totalSprites},");
            sb.AppendLine("  \"scenes\": [");
            WriteSnapshotAssetArray(sb, scenes);
            sb.AppendLine("  ],");
            sb.AppendLine("  \"prefabs\": [");
            WriteSnapshotAssetArray(sb, prefabs);
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(SnapshotPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static void WriteSnapshotAssetArray(StringBuilder sb, IEnumerable<AssetReport> assets)
        {
            var assetList = assets.ToList();
            for (var i = 0; i < assetList.Count; i++)
            {
                var asset = assetList[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"asset_name\": \"{EscapeJson(asset.AssetName)}\",");
                sb.AppendLine($"      \"asset_path\": \"{EscapeJson(asset.AssetPath)}\",");
                sb.AppendLine("      \"sprite_refs\": [");

                for (var j = 0; j < asset.Entries.Count; j++)
                {
                    var entry = asset.Entries[j];
                    sb.AppendLine("        {");
                    sb.AppendLine($"          \"game_object_path\": \"{EscapeJson(entry.GameObjectPath)}\",");
                    sb.AppendLine($"          \"component_type\": \"{EscapeJson(entry.ComponentType)}\",");
                    sb.AppendLine($"          \"field_name\": \"{EscapeJson(entry.FieldName)}\",");
                    sb.AppendLine($"          \"sprite_name\": \"{EscapeJson(entry.SpriteName)}\",");
                    sb.AppendLine($"          \"sprite_path\": \"{EscapeJson(entry.SpritePath)}\",");
                    sb.AppendLine($"          \"guid\": \"{EscapeJson(entry.Guid)}\"");
                    sb.Append("        }");
                    sb.AppendLine(j + 1 < asset.Entries.Count ? "," : string.Empty);
                }

                sb.AppendLine("      ]");
                sb.AppendLine("      ,\"broken_sprite_refs\": [");
                for (var j = 0; j < asset.BrokenEntries.Count; j++)
                {
                    var entry = asset.BrokenEntries[j];
                    sb.AppendLine("        {");
                    sb.AppendLine($"          \"game_object_path\": \"{EscapeJson(entry.GameObjectPath)}\",");
                    sb.AppendLine($"          \"component_type\": \"{EscapeJson(entry.ComponentType)}\",");
                    sb.AppendLine($"          \"field_name\": \"{EscapeJson(entry.FieldName)}\",");
                    sb.AppendLine($"          \"instance_id\": {entry.InstanceId}");
                    sb.Append("        }");
                    sb.AppendLine(j + 1 < asset.BrokenEntries.Count ? "," : string.Empty);
                }
                sb.AppendLine("      ]");
                sb.Append("    }");
                sb.AppendLine(i + 1 < assetList.Count ? "," : string.Empty);
            }
        }

        private static string EscapeJson(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        private sealed class AssetReport
        {
            public string AssetName;
            public string AssetPath;
            public string AssetKind;
            public List<SpriteEntry> Entries = new();
            public List<BrokenSpriteEntry> BrokenEntries = new();
        }

        [Serializable]
        private sealed class SpriteEntry
        {
            public string GameObjectPath;
            public string ComponentType;
            public string FieldName;
            public string SpriteName;
            public string SpritePath;
            public string Guid;
        }

        [Serializable]
        private sealed class BrokenSpriteEntry
        {
            public string GameObjectPath;
            public string ComponentType;
            public string FieldName;
            public int InstanceId;
        }
    }
}
