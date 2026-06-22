using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpenGS.EditorTools
{
    public sealed class PrefabAssetBrowserWindow : EditorWindow
    {
        private const string RootFolder = "Assets/Prefabs";
        private const string ToolbarSearchTextFieldName = "ToolbarSeachTextField";
        private const string ToolbarSearchCancelButtonName = "ToolbarSeachCancelButton";

        private enum ViewMode
        {
            Compact = 0,
            List = 1,
            Grid = 2
        }

        private readonly List<PrefabEntry> allEntries = new();
        private readonly List<PrefabEntry> filteredEntries = new();

        private string searchText = string.Empty;
        private string folderFilter = RootFolder;
        private ViewMode viewMode = ViewMode.Compact;
        private Vector2 scroll;

        [MenuItem("OpenGSR/Tools/Prefab Browser")]
        public static void Open()
        {
            var window = GetWindow<PrefabAssetBrowserWindow>();
            window.titleContent = new GUIContent("Prefab Browser");
            window.minSize = new Vector2(620, 420);
            window.Show();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnProjectChange()
        {
            Refresh();
            Repaint();
        }

        private void Refresh()
        {
            allEntries.Clear();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { RootFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    continue;
                }

                allEntries.Add(new PrefabEntry(asset, path));
            }

            RebuildFiltered();
        }

        private void RebuildFiltered()
        {
            filteredEntries.Clear();

            var search = searchText?.Trim().ToLowerInvariant() ?? string.Empty;
            foreach (var entry in allEntries)
            {
                if (!string.IsNullOrWhiteSpace(folderFilter) && !entry.Folder.StartsWith(folderFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(search) &&
                    !entry.Name.ToLowerInvariant().Contains(search) &&
                    !entry.Path.ToLowerInvariant().Contains(search))
                {
                    continue;
                }

                filteredEntries.Add(entry);
            }

            filteredEntries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space(4);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("Prefab が見つかりませんでした。検索条件を変えるか Refresh してください。", MessageType.Info);
            }
            else
            {
                switch (viewMode)
                {
                    case ViewMode.Compact:
                        DrawCompactList();
                        break;
                    case ViewMode.List:
                        DrawListView();
                        break;
                    default:
                        DrawGrid();
                        break;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Refresh();
                }

                GUILayout.Space(8);
                GUILayout.Label("Folder", GUILayout.Width(40));

                var folders = BuildFolderOptions();
                var currentIndex = Mathf.Max(0, folders.IndexOf(folderFilter));
                var nextIndex = EditorGUILayout.Popup(currentIndex, folders.ToArray(), GUILayout.Width(220));
                folderFilter = folders[Mathf.Clamp(nextIndex, 0, folders.Count - 1)];

                GUILayout.Space(8);
                GUILayout.Label("Search", GUILayout.Width(45));
                var newSearch = GUILayout.TextField(searchText, GUI.skin.FindStyle(ToolbarSearchTextFieldName) ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(160));
                if (newSearch != searchText)
                {
                    searchText = newSearch;
                    RebuildFiltered();
                }

                if (GUILayout.Button(string.Empty, GUI.skin.FindStyle(ToolbarSearchCancelButtonName) ?? EditorStyles.toolbarButton, GUILayout.Width(18)))
                {
                    searchText = string.Empty;
                    RebuildFiltered();
                    GUI.FocusControl(null);
                }

                GUILayout.Space(8);
                var nextViewMode = (ViewMode)GUILayout.Toolbar((int)viewMode, new[] { "Compact", "List", "Grid" }, EditorStyles.toolbarButton, GUILayout.Width(220));
                if (nextViewMode != viewMode)
                {
                    viewMode = nextViewMode;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"{filteredEntries.Count} prefabs", EditorStyles.miniLabel);
            }
        }

        private List<string> BuildFolderOptions()
        {
            var folders = new List<string> { RootFolder };
            folders.AddRange(allEntries
                .Select(entry => entry.Folder)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase));

            if (!folders.Contains(folderFilter, StringComparer.OrdinalIgnoreCase))
            {
                folderFilter = RootFolder;
            }

            return folders;
        }

        private void DrawCompactList()
        {
            foreach (var entry in filteredEntries)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var preview = entry.Preview ?? AssetPreview.GetMiniThumbnail(entry.Asset);
                    GUILayout.Label(preview != null ? preview : EditorGUIUtility.IconContent("Prefab Icon").image, GUILayout.Width(24), GUILayout.Height(24));

                    if (GUILayout.Button(entry.Name, EditorStyles.label))
                    {
                        SelectEntry(entry);
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(entry.Path, EditorStyles.miniLabel);
                }
            }
        }

        private void DrawListView()
        {
            foreach (var entry in filteredEntries)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var preview = entry.Preview ?? AssetPreview.GetMiniThumbnail(entry.Asset);
                    GUILayout.Label(preview != null ? preview : EditorGUIUtility.IconContent("Prefab Icon").image, GUILayout.Width(48), GUILayout.Height(48));

                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (GUILayout.Button(entry.Name, EditorStyles.boldLabel))
                        {
                            SelectEntry(entry);
                        }

                        EditorGUILayout.LabelField(entry.Path, EditorStyles.miniLabel);
                    }
                }
            }
        }

        private void DrawGrid()
        {
            const float tileWidth = 280f;
            const float tileHeight = 86f;

            var columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 16f) / tileWidth));
            var rowCount = Mathf.CeilToInt(filteredEntries.Count / (float)columns);
            var index = 0;

            for (var row = 0; row < rowCount; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (var col = 0; col < columns; col++)
                    {
                        if (index >= filteredEntries.Count)
                        {
                            GUILayout.FlexibleSpace();
                            continue;
                        }

                        DrawTile(filteredEntries[index], tileWidth, tileHeight);
                        index++;
                    }
                }
            }
        }

        private void DrawTile(PrefabEntry entry, float width, float height)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width), GUILayout.Height(height)))
            {
                var rect = GUILayoutUtility.GetRect(width, 50f, GUILayout.ExpandWidth(true));
                var preview = entry.Preview ?? AssetPreview.GetMiniThumbnail(entry.Asset);
                if (preview != null)
                {
                    GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUI.Box(rect, "No Preview");
                }

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    HandleTileClick(entry, Event.current);
                }

                GUILayout.Label(entry.Name, EditorStyles.boldLabel);
                GUILayout.Label(entry.Path, EditorStyles.miniLabel);
            }
        }

        private void HandleTileClick(PrefabEntry entry, Event evt)
        {
            if (evt.clickCount >= 2)
            {
                AssetDatabase.OpenAsset(entry.Asset);
            }
            else
            {
                SelectEntry(entry);
            }

            evt.Use();
        }

        private void SelectEntry(PrefabEntry entry)
        {
            Selection.activeObject = entry.Asset;
            EditorGUIUtility.PingObject(entry.Asset);
        }

        private sealed class PrefabEntry
        {
            public readonly GameObject Asset;
            public readonly string Path;
            public readonly string Name;
            public readonly string Folder;
            public readonly Texture2D Preview;

            public PrefabEntry(GameObject asset, string path)
            {
                Asset = asset;
                Path = path;
                Name = asset.name;
                Folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? RootFolder;
                Preview = AssetPreview.GetAssetPreview(asset);
            }
        }
    }
}
