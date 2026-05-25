#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace OpenGS
{
    public class MapJsonExporterWindow : EditorWindow
    {
        private GameObject sourceRoot;
        private string exportFolder = "Assets/ExportedMaps";
        private string fileName = "MapExport.json";
        private bool includeInactive = true;

        [MenuItem("Tools/Map Json Exporter")]
        public static void ShowWindow()
        {
            GetWindow<MapJsonExporterWindow>("Map Json Exporter");
        }

        private void OnEnable()
        {
            if (sourceRoot == null)
            {
                sourceRoot = Selection.activeGameObject;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            sourceRoot = (GameObject)EditorGUILayout.ObjectField("Root GameObject", sourceRoot, typeof(GameObject), true);
            includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            exportFolder = EditorGUILayout.TextField("Export Folder", exportFolder);
            fileName = EditorGUILayout.TextField("File Name", fileName);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Use Current Selection"))
            {
                sourceRoot = Selection.activeGameObject;
            }

            if (GUILayout.Button("Export JSON"))
            {
                Export();
            }

            if (GUILayout.Button("Open Export Folder"))
            {
                OpenExportFolder();
            }
        }

        private void Export()
        {
            var root = sourceRoot != null ? sourceRoot : Selection.activeGameObject;
            if (root == null)
            {
                Debug.LogWarning("[MapJsonExporter] No root GameObject selected.");
                return;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"{root.name}.json";
            }

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            var data = MapJsonExporter.Export(root, includeInactive);
            var fullFolder = Path.GetFullPath(exportFolder);
            Directory.CreateDirectory(fullFolder);

            var fullPath = Path.Combine(fullFolder, fileName);
            File.WriteAllText(fullPath, data.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[MapJsonExporter] Exported map JSON to {fullPath}");
        }

        private void OpenExportFolder()
        {
            var fullFolder = Path.GetFullPath(exportFolder);
            Directory.CreateDirectory(fullFolder);

#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("explorer.exe", fullFolder);
#elif UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", fullFolder);
#elif UNITY_EDITOR_LINUX
            System.Diagnostics.Process.Start("xdg-open", fullFolder);
#else
            Debug.LogWarning("[MapJsonExporter] Opening folders is not supported on this platform.");
#endif
        }
    }

    public static class MapJsonExporter
    {
        public static JObject Export(GameObject root, bool includeInactive)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var special = new JObject
            {
                ["itemSpawnPoints"] = new JArray(),
                ["respawnPointGroups"] = new JArray(),
                ["flagStands"] = new JArray()
            };

            var rootNode = BuildNode(root.transform, root.transform, includeInactive, special);

            return new JObject
            {
                ["schemaVersion"] = 1,
                ["exportedAtUtc"] = DateTime.UtcNow.ToString("O"),
                ["sceneName"] = root.scene.name,
                ["rootObject"] = root.name,
                ["root"] = rootNode,
                ["specialObjects"] = special
            };
        }

        private static JObject BuildNode(Transform root, Transform current, bool includeInactive, JObject special)
        {
            var node = new JObject
            {
                ["name"] = current.name,
                ["path"] = GetPath(root, current),
                ["activeSelf"] = current.gameObject.activeSelf,
                ["layer"] = current.gameObject.layer,
                ["tag"] = current.gameObject.tag,
                ["localPosition"] = VectorToJson(current.localPosition),
                ["localRotationEuler"] = VectorToJson(current.localEulerAngles),
                ["localScale"] = VectorToJson(current.localScale),
                ["prefab"] = GetPrefabInfo(current.gameObject),
                ["components"] = GetComponentNames(current.gameObject),
                ["spriteRenderers"] = ExportSpriteRenderers(current.gameObject),
                ["colliders"] = ExportColliders(current.gameObject),
                ["children"] = new JArray()
            };

            ExportKnownComponents(current.gameObject, special);

            var children = (JArray)node["children"];
            foreach (Transform child in current)
            {
                if (!includeInactive && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                children.Add(BuildNode(root, child, includeInactive, special));
            }

            return node;
        }

        private static void ExportKnownComponents(GameObject go, JObject special)
        {
            if (go == null)
            {
                return;
            }

            if (go.TryGetComponent<ItemSpawnPoint>(out var itemSpawnPoint))
            {
                ((JArray)special["itemSpawnPoints"]).Add(ExportItemSpawnPoint(go, itemSpawnPoint));
            }

            if (go.TryGetComponent<ReSpawnPoints>(out var respawnPoints))
            {
                ((JArray)special["respawnPointGroups"]).Add(ExportRespawnPoints(go, respawnPoints));
            }

            if (go.TryGetComponent<FlagStand>(out var flagStand))
            {
                ((JArray)special["flagStands"]).Add(ExportFlagStand(go, flagStand));
            }
        }

        private static JArray ExportSpriteRenderers(GameObject go)
        {
            var result = new JArray();
            if (go == null)
            {
                return result;
            }

            foreach (var renderer in go.GetComponents<SpriteRenderer>())
            {
                if (renderer == null)
                {
                    continue;
                }

                result.Add(new JObject
                {
                    ["type"] = renderer.GetType().Name,
                    ["enabled"] = renderer.enabled,
                    ["sprite"] = GetAssetReference(renderer.sprite),
                    ["color"] = ColorToJson(renderer.color),
                    ["sortingLayerID"] = renderer.sortingLayerID,
                    ["sortingLayerName"] = renderer.sortingLayerName,
                    ["sortingOrder"] = renderer.sortingOrder,
                    ["flipX"] = renderer.flipX,
                    ["flipY"] = renderer.flipY,
                    ["drawMode"] = renderer.drawMode.ToString(),
                    ["size"] = VectorToJson(renderer.size),
                    ["maskInteraction"] = renderer.maskInteraction.ToString()
                });
            }

            return result;
        }

        private static JArray ExportColliders(GameObject go)
        {
            var result = new JArray();
            if (go == null)
            {
                return result;
            }

            foreach (var collider in go.GetComponents<Collider2D>())
            {
                if (collider == null)
                {
                    continue;
                }

                var entry = new JObject
                {
                    ["type"] = collider.GetType().Name,
                    ["enabled"] = collider.enabled,
                    ["isTrigger"] = collider.isTrigger,
                    ["offset"] = VectorToJson(collider.offset),
                    ["sharedMaterial"] = GetAssetReference(collider.sharedMaterial)
                };

                if (collider is BoxCollider2D box)
                {
                    entry["size"] = VectorToJson(box.size);
                    entry["edgeRadius"] = box.edgeRadius;
                    entry["usedByEffector"] = box.usedByEffector;
#pragma warning disable 0618
                    entry["usedByComposite"] = box.usedByComposite;
#pragma warning restore 0618
                }
                else if (collider is CircleCollider2D circle)
                {
                    entry["radius"] = circle.radius;
                    entry["usedByEffector"] = circle.usedByEffector;
#pragma warning disable 0618
                    entry["usedByComposite"] = circle.usedByComposite;
#pragma warning restore 0618
                }
                else if (collider is CapsuleCollider2D capsule)
                {
                    entry["size"] = VectorToJson(capsule.size);
                    entry["direction"] = capsule.direction.ToString();
                    entry["usedByEffector"] = capsule.usedByEffector;
#pragma warning disable 0618
                    entry["usedByComposite"] = capsule.usedByComposite;
#pragma warning restore 0618
                }
                else if (collider is EdgeCollider2D edge)
                {
                    entry["edgeRadius"] = edge.edgeRadius;
                    entry["points"] = PointsToJson(edge.points);
                }
                else if (collider is PolygonCollider2D polygon)
                {
                    entry["pathCount"] = polygon.pathCount;
                    var paths = new JArray();
                    for (var i = 0; i < polygon.pathCount; i++)
                    {
                        paths.Add(PointsToJson(polygon.GetPath(i)));
                    }
                    entry["paths"] = paths;
                }

                result.Add(entry);
            }

            return result;
        }

        private static JObject ExportItemSpawnPoint(GameObject go, ItemSpawnPoint itemSpawnPoint)
        {
            return new JObject
            {
                ["name"] = go.name,
                ["path"] = GetPathFromRoot(go.transform.root, go.transform),
                ["position"] = VectorToJson(go.transform.position),
                ["localPosition"] = VectorToJson(go.transform.localPosition),
                ["spawnPointId"] = GetFieldValue<int>(itemSpawnPoint, "spawnPointId"),
                ["heightOffset"] = GetFieldValue<float>(itemSpawnPoint, "heightOffset"),
                ["startImmediately"] = GetFieldValue<bool>(itemSpawnPoint, "startImmidietry"),
                ["firstTimeDelay"] = GetFieldValue<float>(itemSpawnPoint, "firstTimeDelay"),
                ["generateInterval"] = GetFieldValue<float>(itemSpawnPoint, "generateInterval"),
                ["prefabs"] = new JObject
                {
                    ["powerUpItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "powerUpItemPrefab")),
                    ["defenceUpItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "defenceUpItemPrefab")),
                    ["speedUpItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "speedUpItemPrefab")),
                    ["stealthItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "stealthItemPrefab")),
                    ["grenadePackItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "grenadePackItemPrefab")),
                    ["healItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "healItemPrefab")),
                    ["randomItemPrefab"] = GetAssetReference(GetFieldValue<GameObject>(itemSpawnPoint, "randomItemPrefab"))
                }
            };
        }

        private static JObject ExportRespawnPoints(GameObject go, ReSpawnPoints respawnPoints)
        {
            var points = new JArray();
            var pointNames = new JArray();

            if (respawnPoints.Points != null)
            {
                foreach (var point in respawnPoints.Points)
                {
                    if (point == null)
                    {
                        continue;
                    }

                    points.Add(new JObject
                    {
                        ["name"] = point.name,
                        ["path"] = GetPathFromRoot(go.transform.root, point.transform),
                        ["position"] = VectorToJson(point.transform.position),
                        ["localPosition"] = VectorToJson(point.transform.localPosition)
                    });
                    pointNames.Add(point.name);
                }
            }

            return new JObject
            {
                ["name"] = go.name,
                ["path"] = GetPathFromRoot(go.transform.root, go.transform),
                ["dontUseBeforePoint"] = respawnPoints.dontUseBeforePoint,
                ["count"] = respawnPoints.Count(),
                ["points"] = points,
                ["pointNames"] = pointNames
            };
        }

        private static JObject ExportFlagStand(GameObject go, FlagStand flagStand)
        {
            return new JObject
            {
                ["name"] = go.name,
                ["path"] = GetPathFromRoot(go.transform.root, go.transform),
                ["position"] = VectorToJson(go.transform.position),
                ["team"] = GetFieldValue<object>(flagStand, "team")?.ToString(),
                ["showFlagNavigator"] = GetFieldValue<bool>(flagStand, "showFlagNavigator"),
                ["hasFlag"] = flagStand.HasFlag(),
                ["flagSlotPath"] = GetHierarchyPath(go.transform.root, GetFieldValue<GameObject>(flagStand, "flagSlot")?.transform),
                ["flagNavigatorPath"] = GetHierarchyPath(go.transform.root, GetFieldValue<GameObject>(flagStand, "flagNavigator")?.transform),
                ["flagMasterData"] = GetAssetReference(GetFieldValue<ScriptableObject>(flagStand, "flagMasterData"))
            };
        }

        private static JArray GetComponentNames(GameObject go)
        {
            var components = new JArray();
            if (go == null)
            {
                return components;
            }

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                components.Add(component.GetType().Name);
            }

            return components;
        }

        private static JObject GetPrefabInfo(GameObject go)
        {
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                Debug.LogWarning($"[MapJsonExporter] No prefab path found for {go?.name ?? "<null>"}.");
                return null;
            }

            return new JObject
            {
                ["path"] = prefabPath,
                ["guid"] = AssetDatabase.AssetPathToGUID(prefabPath),
                ["name"] = Path.GetFileNameWithoutExtension(prefabPath)
            };
        }

        private static JObject GetAssetReference(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("[MapJsonExporter] GetAssetReference received null asset.");
                return null;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            return new JObject
            {
                ["name"] = asset.name,
                ["path"] = path,
                ["guid"] = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path)
            };
        }

        private static JObject VectorToJson(Vector3 value)
        {
            return new JObject
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z
            };
        }

        private static JObject VectorToJson(Vector2 value)
        {
            return new JObject
            {
                ["x"] = value.x,
                ["y"] = value.y
            };
        }

        private static JObject ColorToJson(Color value)
        {
            return new JObject
            {
                ["r"] = value.r,
                ["g"] = value.g,
                ["b"] = value.b,
                ["a"] = value.a
            };
        }

        private static JArray PointsToJson(Vector2[] points)
        {
            var result = new JArray();
            if (points == null)
            {
                return result;
            }

            foreach (var point in points)
            {
                result.Add(VectorToJson(point));
            }

            return result;
        }

        private static string GetPath(Transform root, Transform current)
        {
            if (root == null || current == null)
            {
                return string.Empty;
            }

            if (root == current)
            {
                return current.name;
            }

            var stack = new Stack<string>();
            var cursor = current;
            while (cursor != null)
            {
                stack.Push(cursor.name);
                if (cursor == root)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return string.Join("/", stack);
        }

        private static string GetPathFromRoot(Transform root, Transform current)
        {
            return GetHierarchyPath(root, current);
        }

        private static string GetHierarchyPath(Transform root, Transform current)
        {
            if (current == null)
            {
                Debug.LogWarning("[MapJsonExporter] GetHierarchyPath received null current transform.");
                return null;
            }

            if (root == null)
            {
                return current.name;
            }

            if (current == root)
            {
                return current.name;
            }

            var stack = new Stack<string>();
            var cursor = current;
            while (cursor != null)
            {
                stack.Push(cursor.name);
                if (cursor == root)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return string.Join("/", stack);
        }

        private static T GetFieldValue<T>(object target, string fieldName)
        {
            if (target == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return default;
            }

            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    var value = field.GetValue(target);
                    if (value is T typedValue)
                    {
                        return typedValue;
                    }

                    if (value != null)
                    {
                        try
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            return default;
                        }
                    }
                }

                type = type.BaseType;
            }

            return default;
        }
    }
}
#endif
