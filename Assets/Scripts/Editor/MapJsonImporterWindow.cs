#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenGS
{
    public class MapJsonImporterWindow : EditorWindow
    {
        private string jsonFilePath = "Assets/ExportedMaps/MapExport.json";
        private GameObject targetParent;
        private bool createRootObject = true;

        [MenuItem("Tools/Map Json Importer")]
        public static void ShowWindow()
        {
            GetWindow<MapJsonImporterWindow>("Map Json Importer");
        }

        private void OnEnable()
        {
            if (targetParent == null)
            {
                targetParent = Selection.activeGameObject;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            jsonFilePath = EditorGUILayout.TextField("JSON File", jsonFilePath);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            targetParent = (GameObject)EditorGUILayout.ObjectField("Parent", targetParent, typeof(GameObject), true);
            createRootObject = EditorGUILayout.Toggle("Create Root Object", createRootObject);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Use Current Selection"))
            {
                targetParent = Selection.activeGameObject;
            }

            if (GUILayout.Button("Import JSON"))
            {
                Import();
            }

            if (GUILayout.Button("Browse JSON"))
            {
                BrowseJson();
            }
        }

        private void Import()
        {
            var fullPath = Path.GetFullPath(jsonFilePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[MapJsonImporter] File not found: {fullPath}");
                return;
            }

            JObject json;
            try
            {
                json = JObject.Parse(File.ReadAllText(fullPath));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapJsonImporter] Failed to parse JSON: {ex.Message}");
                return;
            }

            var rootNode = json["root"] as JObject;
            if (rootNode == null)
            {
                Debug.LogError("[MapJsonImporter] JSON does not contain a root node.");
                return;
            }

            var rootName = json["rootObject"]?.ToString();
            if (string.IsNullOrWhiteSpace(rootName))
            {
                rootName = rootNode["name"]?.ToString() ?? "ImportedMap";
            }

            GameObject rootObject;
            if (createRootObject)
            {
                rootObject = new GameObject(rootName);
                Undo.RegisterCreatedObjectUndo(rootObject, "Import Map JSON");
                if (targetParent != null)
                {
                    Undo.SetTransformParent(rootObject.transform, targetParent.transform, "Import Map JSON");
                }
            }
            else
            {
                rootObject = targetParent != null ? targetParent : new GameObject(rootName);
                if (targetParent == null)
                {
                    Undo.RegisterCreatedObjectUndo(rootObject, "Import Map JSON");
                }
                rootObject.name = rootName;
            }

            ApplyNode(rootObject, rootNode);
            EditorSceneManager.MarkSceneDirty(rootObject.scene);
            Selection.activeGameObject = rootObject;

            Debug.Log($"[MapJsonImporter] Imported map JSON from {fullPath}");
        }

        private void ApplyNode(GameObject go, JObject node)
        {
            if (go == null || node == null)
            {
                return;
            }

            go.name = node["name"]?.ToString() ?? go.name;
            var tag = node["tag"]?.ToString();
            if (!string.IsNullOrWhiteSpace(tag))
            {
                try
                {
                    go.tag = tag;
                }
                catch (UnityException)
                {
                    Debug.LogWarning($"[MapJsonImporter] Tag '{tag}' is not defined in this project.");
                }
            }
            go.layer = node["layer"]?.Value<int>() ?? go.layer;
            go.SetActive(node["activeSelf"]?.Value<bool>() ?? true);

            ApplyTransform(go.transform, node);
            ApplySpriteRenderers(go, node["spriteRenderers"] as JArray);
            ApplyColliders(go, node["colliders"] as JArray);

            var children = node["children"] as JArray;
            if (children == null)
            {
                return;
            }

            foreach (var childToken in children.OfType<JObject>())
            {
                var childName = childToken["name"]?.ToString() ?? "Child";
                var childGo = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(childGo, "Import Map JSON Child");
                Undo.SetTransformParent(childGo.transform, go.transform, "Import Map JSON Child");
                ApplyNode(childGo, childToken);
            }
        }

        private static void ApplyTransform(Transform transform, JObject node)
        {
            if (transform == null || node == null)
            {
                return;
            }

            transform.localPosition = ReadVector3(node["localPosition"]);
            transform.localEulerAngles = ReadVector3(node["localRotationEuler"]);
            transform.localScale = ReadVector3(node["localScale"], Vector3.one);
        }

        private static void ApplySpriteRenderers(GameObject go, JArray renderersJson)
        {
            if (go == null || renderersJson == null)
            {
                return;
            }

            var existing = go.GetComponents<SpriteRenderer>().ToList();
            var index = 0;

            foreach (var token in renderersJson.OfType<JObject>())
            {
                SpriteRenderer renderer;
                if (index < existing.Count)
                {
                    renderer = existing[index];
                }
                else
                {
                    renderer = Undo.AddComponent(go, typeof(SpriteRenderer)) as SpriteRenderer;
                }

                ApplySpriteRenderer(renderer, token);
                index++;
            }
        }

        private static void ApplySpriteRenderer(SpriteRenderer renderer, JObject json)
        {
            if (renderer == null || json == null)
            {
                return;
            }

            renderer.enabled = json["enabled"]?.Value<bool>() ?? renderer.enabled;
            renderer.sprite = LoadAssetReference<Sprite>(json["sprite"] as JObject);
            renderer.color = ReadColor(json["color"], renderer.color);
            renderer.sortingLayerID = json["sortingLayerID"]?.Value<int>() ?? renderer.sortingLayerID;
            renderer.sortingLayerName = json["sortingLayerName"]?.ToString() ?? renderer.sortingLayerName;
            renderer.sortingOrder = json["sortingOrder"]?.Value<int>() ?? renderer.sortingOrder;
            renderer.flipX = json["flipX"]?.Value<bool>() ?? renderer.flipX;
            renderer.flipY = json["flipY"]?.Value<bool>() ?? renderer.flipY;

            if (Enum.TryParse(json["drawMode"]?.ToString(), out SpriteDrawMode drawMode))
            {
                renderer.drawMode = drawMode;
            }

            renderer.size = ReadVector2(json["size"], renderer.size);

            if (Enum.TryParse(json["maskInteraction"]?.ToString(), out SpriteMaskInteraction maskInteraction))
            {
                renderer.maskInteraction = maskInteraction;
            }
        }

        private static void ApplyColliders(GameObject go, JArray collidersJson)
        {
            if (go == null || collidersJson == null)
            {
                return;
            }

            var existing = go.GetComponents<Collider2D>().ToList();
            var used = new HashSet<Collider2D>();

            foreach (var token in collidersJson.OfType<JObject>())
            {
                var colliderType = ResolveColliderType(token["type"]?.ToString());
                if (colliderType == null)
                {
                    Debug.LogWarning($"[MapJsonImporter] Unknown collider type: {token["type"]}");
                    continue;
                }

                var collider = existing.FirstOrDefault(c => !used.Contains(c) && c != null && c.GetType() == colliderType);
                if (collider == null)
                {
                    collider = (Collider2D)Undo.AddComponent(go, colliderType);
                }

                used.Add(collider);
                ApplyCollider(collider, token);
            }
        }

        private static void ApplyCollider(Collider2D collider, JObject json)
        {
            if (collider == null || json == null)
            {
                return;
            }

            collider.enabled = json["enabled"]?.Value<bool>() ?? collider.enabled;
            collider.isTrigger = json["isTrigger"]?.Value<bool>() ?? collider.isTrigger;
            collider.offset = ReadVector2(json["offset"], collider.offset);
            collider.sharedMaterial = LoadAssetReference<PhysicsMaterial2D>(json["sharedMaterial"] as JObject);

            switch (collider)
            {
                case BoxCollider2D box:
                    box.size = ReadVector2(json["size"], box.size);
                    box.edgeRadius = json["edgeRadius"]?.Value<float>() ?? box.edgeRadius;
                    box.usedByEffector = json["usedByEffector"]?.Value<bool>() ?? box.usedByEffector;
                    box.usedByComposite = json["usedByComposite"]?.Value<bool>() ?? box.usedByComposite;
                    break;
                case CircleCollider2D circle:
                    circle.radius = json["radius"]?.Value<float>() ?? circle.radius;
                    circle.usedByEffector = json["usedByEffector"]?.Value<bool>() ?? circle.usedByEffector;
                    circle.usedByComposite = json["usedByComposite"]?.Value<bool>() ?? circle.usedByComposite;
                    break;
                case CapsuleCollider2D capsule:
                    capsule.size = ReadVector2(json["size"], capsule.size);
                    if (json["direction"] != null && Enum.TryParse(json["direction"].ToString(), out CapsuleDirection2D direction))
                    {
                        capsule.direction = direction;
                    }
                    capsule.usedByEffector = json["usedByEffector"]?.Value<bool>() ?? capsule.usedByEffector;
                    capsule.usedByComposite = json["usedByComposite"]?.Value<bool>() ?? capsule.usedByComposite;
                    break;
                case EdgeCollider2D edge:
                    edge.edgeRadius = json["edgeRadius"]?.Value<float>() ?? edge.edgeRadius;
                    edge.points = ReadPoints(json["points"] as JArray);
                    break;
                case PolygonCollider2D polygon:
                    ApplyPolygonCollider(polygon, json["paths"] as JArray);
                    break;
            }
        }

        private static void ApplyPolygonCollider(PolygonCollider2D polygon, JArray pathsJson)
        {
            if (polygon == null || pathsJson == null)
            {
                return;
            }

            var paths = pathsJson.OfType<JArray>().Select(ReadPoints).ToArray();
            polygon.pathCount = paths.Length;
            for (var i = 0; i < paths.Length; i++)
            {
                polygon.SetPath(i, paths[i]);
            }
        }

        private static Type ResolveColliderType(string typeName)
        {
            return typeName switch
            {
                "BoxCollider2D" => typeof(BoxCollider2D),
                "CircleCollider2D" => typeof(CircleCollider2D),
                "CapsuleCollider2D" => typeof(CapsuleCollider2D),
                "EdgeCollider2D" => typeof(EdgeCollider2D),
                "PolygonCollider2D" => typeof(PolygonCollider2D),
                _ => null
            };
        }

        private static Vector3 ReadVector3(JToken token, Vector3 fallback = default)
        {
            if (token is not JObject obj)
            {
                return fallback;
            }

            return new Vector3(
                obj["x"]?.Value<float>() ?? fallback.x,
                obj["y"]?.Value<float>() ?? fallback.y,
                obj["z"]?.Value<float>() ?? fallback.z);
        }

        private static Vector2 ReadVector2(JToken token, Vector2 fallback = default)
        {
            if (token is not JObject obj)
            {
                return fallback;
            }

            return new Vector2(
                obj["x"]?.Value<float>() ?? fallback.x,
                obj["y"]?.Value<float>() ?? fallback.y);
        }

        private static Color ReadColor(JToken token, Color fallback)
        {
            if (token is not JObject obj)
            {
                return fallback;
            }

            return new Color(
                obj["r"]?.Value<float>() ?? fallback.r,
                obj["g"]?.Value<float>() ?? fallback.g,
                obj["b"]?.Value<float>() ?? fallback.b,
                obj["a"]?.Value<float>() ?? fallback.a);
        }

        private static Vector2[] ReadPoints(JArray pointsJson)
        {
            if (pointsJson == null)
            {
                return Array.Empty<Vector2>();
            }

            var points = new List<Vector2>();
            foreach (var pointToken in pointsJson.OfType<JObject>())
            {
                points.Add(ReadVector2(pointToken));
            }

            return points.ToArray();
        }

        private static T LoadAssetReference<T>(JObject assetRef) where T : UnityEngine.Object
        {
            if (assetRef == null)
            {
                return null;
            }

            var path = assetRef["path"]?.ToString();
            if (!string.IsNullOrWhiteSpace(path))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            var guid = assetRef["guid"]?.ToString();
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    return AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
            }

            return null;
        }

        private void BrowseJson()
        {
            var selected = EditorUtility.OpenFilePanel("Select Map JSON", Application.dataPath, "json");
            if (!string.IsNullOrWhiteSpace(selected))
            {
                jsonFilePath = selected;
            }
        }
    }
}
#endif
