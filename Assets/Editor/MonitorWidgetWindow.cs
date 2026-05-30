using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS.EditorTools
{
    /// <summary>
    /// UnityEditor 上で使う軽量な監視ウィジェット。
    /// 選択中のオブジェクトをピン留めして、Play Mode 中の状態を見やすく表示する。
    /// </summary>
    public sealed class MonitorWidgetWindow : EditorWindow
    {
        [Serializable]
        private sealed class WatchItem
        {
            public UnityEngine.Object Target;
            public string Label;
            public bool Expanded = true;
        }

        [SerializeField] private List<WatchItem> watchItems = new List<WatchItem>();
        [SerializeField] private bool autoAddSelectionAsWatch = true;
        [SerializeField] private bool showSelectionPreview = true;
        [SerializeField] private bool showHierarchyPath = true;
        [SerializeField] private bool showTransformDetails = true;
        [SerializeField] private bool showComponentNames = true;
        [SerializeField] private bool repaintWhilePlaying = true;
        [SerializeField] private float repaintInterval = 0.25f;

        private Vector2 scroll;
        private double nextRepaintTime;
        private UnityEngine.Object pendingAddTarget;

        [MenuItem("OpenGSR/Tools/Monitor Widget")]
        private static void Open()
        {
            var window = GetWindow<MonitorWidgetWindow>();
            window.titleContent = new GUIContent("Monitor");
            window.minSize = new Vector2(520f, 280f);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += OnEditorUpdate;
            CacheSelectionPreview();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnSelectionChanged()
        {
            if (autoAddSelectionAsWatch)
            {
                pendingAddTarget = Selection.activeObject;
            }

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (pendingAddTarget != null)
            {
                AddWatch(pendingAddTarget);
                pendingAddTarget = null;
            }

            if (!Application.isPlaying || !repaintWhilePlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < nextRepaintTime)
            {
                return;
            }

            nextRepaintTime = EditorApplication.timeSinceStartup + Math.Max(0.05, repaintInterval);
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawStatusPanel();
            EditorGUILayout.Space(6);

            if (showSelectionPreview)
            {
                DrawSelectionPanel();
                EditorGUILayout.Space(6);
            }

            DrawWatchList();

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Add Selection", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    AddWatch(Selection.activeObject);
                }

                if (GUILayout.Button("Add Scene Obj", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    AddWatch(Selection.activeGameObject);
                }

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    watchItems.Clear();
                }

                GUILayout.FlexibleSpace();

                autoAddSelectionAsWatch = GUILayout.Toggle(autoAddSelectionAsWatch, "Auto Add Selection", EditorStyles.toolbarButton);
                showSelectionPreview = GUILayout.Toggle(showSelectionPreview, "Selection", EditorStyles.toolbarButton);
                showTransformDetails = GUILayout.Toggle(showTransformDetails, "Transform", EditorStyles.toolbarButton);
                showComponentNames = GUILayout.Toggle(showComponentNames, "Components", EditorStyles.toolbarButton);
            }
        }

        private void DrawStatusPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Play Mode", Application.isPlaying ? "Playing" : "Edit");
                EditorGUILayout.LabelField("Active Scene", SceneManager.GetActiveScene().name);
                EditorGUILayout.LabelField("Selected", Selection.activeObject != null ? Selection.activeObject.name : "-");
                EditorGUILayout.LabelField("Pinned", watchItems.Count.ToString());
            }
        }

        private void DrawSelectionPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);

                var active = Selection.activeObject;
                if (active == null)
                {
                    EditorGUILayout.LabelField("Nothing selected.");
                    return;
                }

                DrawObjectSummary(active, canRemove: false);
            }
        }

        private void DrawWatchList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Watched Objects", EditorStyles.boldLabel);

                if (watchItems.Count == 0)
                {
                    EditorGUILayout.LabelField("No pinned objects yet.");
                    return;
                }

                for (var index = 0; index < watchItems.Count; index++)
                {
                    var item = watchItems[index];
                    if (item == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            item.Expanded = EditorGUILayout.Foldout(item.Expanded, item.Label ?? "(unnamed)", true);
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                            {
                                EditorGUIUtility.PingObject(item.Target);
                            }

                            if (GUILayout.Button("Select", GUILayout.Width(56f)))
                            {
                                Selection.activeObject = item.Target;
                            }

                            if (GUILayout.Button("X", GUILayout.Width(24f)))
                            {
                                watchItems.RemoveAt(index);
                                GUIUtility.ExitGUI();
                            }
                        }

                        if (!item.Expanded)
                        {
                            continue;
                        }

                        if (item.Target == null)
                        {
                            EditorGUILayout.HelpBox("Target is missing.", MessageType.Warning);
                            continue;
                        }

                        DrawObjectSummary(item.Target, canRemove: false);
                    }
                }
            }
        }

        private void DrawObjectSummary(UnityEngine.Object target, bool canRemove)
        {
            EditorGUILayout.ObjectField("Target", target, typeof(UnityEngine.Object), true);
            EditorGUILayout.LabelField("Type", target.GetType().Name);

            var go = GetGameObject(target);
            if (go != null)
            {
                EditorGUILayout.LabelField("GameObject", go.name);
                EditorGUILayout.LabelField("Active", go.activeInHierarchy ? "Yes" : "No");

                if (showHierarchyPath)
                {
                    EditorGUILayout.LabelField("Hierarchy", GetHierarchyPath(go.transform));
                }

                if (showTransformDetails)
                {
                    DrawTransformDetails(go.transform);
                }

                if (showComponentNames)
                {
                    DrawComponentNames(go);
                }
            }

            if (canRemove)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove Watch", GUILayout.Width(110f)))
                    {
                        watchItems.RemoveAll(w => w != null && w.Target == target);
                    }
                }
            }
        }

        private void DrawTransformDetails(Transform transform)
        {
            EditorGUILayout.LabelField("Position", FormatVector3(transform.position));
            EditorGUILayout.LabelField("Local Pos", FormatVector3(transform.localPosition));
            EditorGUILayout.LabelField("Rotation", FormatVector3(transform.eulerAngles));
            EditorGUILayout.LabelField("Local Scale", FormatVector3(transform.localScale));

            if (transform is RectTransform rectTransform)
            {
                EditorGUILayout.LabelField("Anchored Pos", FormatVector3(rectTransform.anchoredPosition3D));
                EditorGUILayout.LabelField("Size Delta", FormatVector2(rectTransform.sizeDelta));
            }
        }

        private void DrawComponentNames(GameObject go)
        {
            var components = go.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType().Name)
                .ToArray();

            EditorGUILayout.LabelField("Components", components.Length > 0 ? string.Join(", ", components) : "-");
        }

        private void AddWatch(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (watchItems.Any(item => item != null && item.Target == target))
            {
                return;
            }

            watchItems.Add(new WatchItem
            {
                Target = target,
                Label = GetWatchLabel(target),
                Expanded = true,
            });
        }

        private void CacheSelectionPreview()
        {
            if (autoAddSelectionAsWatch && Selection.activeObject != null)
            {
                pendingAddTarget = Selection.activeObject;
            }
        }

        private static GameObject GetGameObject(UnityEngine.Object target)
        {
            switch (target)
            {
                case GameObject gameObject:
                    return gameObject;
                case Component component:
                    return component.gameObject;
                default:
                    return null;
            }
        }

        private static string GetWatchLabel(UnityEngine.Object target)
        {
            var go = GetGameObject(target);
            if (go != null)
            {
                return $"{go.name} ({target.GetType().Name})";
            }

            return target != null ? $"{target.name} ({target.GetType().Name})" : "(null)";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            var current = transform;

            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static string FormatVector2(Vector2 value)
        {
            return $"({value.x:F2}, {value.y:F2})";
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
        }
    }
}
