using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OpenGS.EditorTools
{
    public sealed class SpriteRenameAuditWindow : EditorWindow
    {
        private const string ReportDir = "Assets/Reports";
        private const string ReportPath = "Assets/Reports/SpriteRenameAudit.md";

        [MenuItem("OpenGSR/Tools/Sprite Rename Audit")]
        private static void Open()
        {
            var window = GetWindow<SpriteRenameAuditWindow>();
            window.titleContent = new GUIContent("Sprite Rename");
            window.minSize = new Vector2(520, 240);
            window.Show();
        }

        private Vector2 _scroll;
        private string _lastSummary = "Not generated yet.";

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sprite Rename Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans active root sprites and reports whether they appear safe to rename. "
                + "This is meant for root assets that are still kept in place.",
                MessageType.Info);

            if (GUILayout.Button("Generate Audit"))
            {
                Generate();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Output", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_lastSummary, GUILayout.Height(120));
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            if (!Directory.Exists(ReportDir))
            {
                Directory.CreateDirectory(ReportDir);
            }

            var rootSpriteDir = "Assets/Sprites";
            var spriteFiles = AssetDatabase.FindAssets("t:Sprite", new[] { rootSpriteDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => IsRootPng(path))
                .OrderBy(path => path)
                .ToList();

            var rows = new List<AuditRow>();
            foreach (var path in spriteFiles)
            {
                rows.Add(CreateRow(path));
            }

            WriteReport(rows);

            var safeCount = rows.Count(r => r.Status == "safe_candidate");
            var blockedCount = rows.Count - safeCount;
            _lastSummary =
                $"Rows: {rows.Count}\n" +
                $"Safe candidate: {safeCount}\n" +
                $"Blocked/keep: {blockedCount}\n" +
                $"Markdown: {ReportPath}";
        }

        private static bool IsRootPng(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith("Assets/Sprites/", StringComparison.OrdinalIgnoreCase)) return false;
            if (path.IndexOf("/Archive/", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (path.IndexOf('/', "Assets/Sprites/".Length) >= 0) return false;
            return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        private static AuditRow CreateRow(string path)
        {
            var fileName = Path.GetFileName(path);
            var stem = Path.GetFileNameWithoutExtension(path);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var exactPathHits = FindMatches(path);
            var exactFileHits = FindMatches(fileName);
            var exactStemHits = FindMatches(stem);
            var exactPathRefs = exactPathHits.Count;
            var exactFileRefs = exactFileHits.Count;
            var exactStemRefs = exactStemHits.Count;
            var status = (exactPathRefs == 0 && exactFileRefs == 0 && exactStemRefs == 0)
                ? "safe_candidate"
                : "keep_in_place";

            return new AuditRow
            {
                Path = path,
                Name = fileName,
                Guid = guid,
                PathRefs = exactPathRefs,
                FileRefs = exactFileRefs,
                StemRefs = exactStemRefs,
                PathHitFiles = exactPathHits,
                FileHitFiles = exactFileHits,
                StemHitFiles = exactStemHits,
                Status = status,
            };
        }

        private static List<string> FindMatches(string needle)
        {
            if (string.IsNullOrEmpty(needle))
            {
                return new List<string>();
            }

            var hits = new List<string>();
            var files = Directory.GetFiles("X:\\Dev\\OpenGSR", "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    return ext is ".cs" or ".unity" or ".asset" or ".prefab" or ".md" or ".json" or ".txt" or ".asmdef" or ".xml";
                });

            foreach (var file in files)
            {
                try
                {
                    var text = File.ReadAllText(file);
                    if (text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hits.Add(NormalizePath(file));
                    }
                }
                catch
                {
                    // ignore unreadable files
                }
            }

            return hits.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        }

        private static void WriteReport(List<AuditRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Sprite Rename Audit");
            sb.AppendLine();
            sb.AppendLine("This report classifies active root sprites by whether they appear safe to rename.");
            sb.AppendLine("A sprite is only marked safe if there are no exact path, filename, or stem hits in text assets.");
            sb.AppendLine();
            sb.AppendLine("| Sprite | GUID | Path refs | File refs | Stem refs | Status |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- |");

            foreach (var row in rows)
            {
                sb.AppendLine($"| `{row.Path}` | `{row.Guid}` | {row.PathRefs} | {row.FileRefs} | {row.StemRefs} | `{row.Status}` |");
                WriteHitList(sb, "Path hits", row.PathHitFiles);
                WriteHitList(sb, "File hits", row.FileHitFiles);
                WriteHitList(sb, "Stem hits", row.StemHitFiles);
            }

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static void WriteHitList(StringBuilder sb, string label, IReadOnlyList<string> hits)
        {
            if (hits == null || hits.Count == 0)
            {
                return;
            }

            sb.AppendLine($"- {label}:");
            foreach (var hit in hits.Take(10))
            {
                sb.AppendLine($"  - `{hit}`");
            }
            if (hits.Count > 10)
            {
                sb.AppendLine($"  - ... and {hits.Count - 10} more");
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace("X:\\Dev\\OpenGSR\\", "").Replace('\\', '/');
        }

        [Serializable]
        private sealed class AuditRow
        {
            public string Path;
            public string Name;
            public string Guid;
            public int PathRefs;
            public int FileRefs;
            public int StemRefs;
            public List<string> PathHitFiles;
            public List<string> FileHitFiles;
            public List<string> StemHitFiles;
            public string Status;
        }
    }
}
