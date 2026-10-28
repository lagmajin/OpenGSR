#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    public sealed class NativeMapPreviewBuilderWindow : EditorWindow
    {
        private const string DefaultOutputRoot = "Assets/GeneratedMapPreviews";
        private const string DefaultSpriteSearchRoot = "Assets/Sprites/Stages";
        private const float NativeOverviewAspect = (16f / 10f) * (1.68f / 1.30f);

        private string mapJsonPath = @"J:\dev\OpenGSNativeClient\assets\maps\Waterfall.json";
        private string outputRoot = DefaultOutputRoot;
        private string spriteSearchRoot = DefaultSpriteSearchRoot;
        private string lastBuiltScenePath;
        private Vector2 scrollPosition;

        [MenuItem("Tools/OpenGS/Build Native Map Preview")]
        public static void ShowWindow()
        {
            GetWindow<NativeMapPreviewBuilderWindow>("Native Map Preview");
        }

        [MenuItem("Tools/OpenGS/Build Recovered MPF Map Previews")]
        public static void BuildRecoveredMpfPreviews()
        {
            const string sourceRoot = @"J:\dev\OpenGSNativeClient\assets\maps";
            var mapIds = RecoveredMpfMapIds();
            var results = new List<string>();
            foreach (var id in mapIds)
            {
                var outputFolder = $"Assets/GeneratedMapPreviews/{SafeName($"local-original-{id}")}";
                if (AssetDatabase.IsValidFolder(outputFolder)) continue;
                var source = Path.Combine(sourceRoot, $"local-original-{id}.json");
                results.Add(BuildPreview(source));
            }
            Debug.Log($"[NativeMapPreview] Built {results.Count} missing recovered MPF maps; existing previews were preserved and test maps were excluded.\n" +
                      string.Join("\n", results));
            EditorUtility.DisplayDialog("Recovered MPF previews built", $"Built {results.Count} missing isolated map preview scenes. Existing previews were preserved. See the Console for paths.", "OK");
        }

        [MenuItem("Tools/OpenGS/Rebuild Recovered MPF Previews")]
        public static void RebuildRecoveredMpfPreviews()
        {
            var mapIds = RecoveredMpfMapIds();
            foreach (var id in mapIds)
            {
                var outputFolder = $"Assets/GeneratedMapPreviews/{SafeName($"local-original-{id}")}";
                if (AssetDatabase.IsValidFolder(outputFolder))
                {
                    if (!AssetDatabase.DeleteAsset(outputFolder))
                        throw new IOException($"Could not replace generated preview folder {outputFolder}.");
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildRecoveredMpfPreviews();
        }

        private static string[] RecoveredMpfMapIds() => new[]
        {
            "001-pipe", "001-pipectf", "002-green", "002-greenctf", "003-city", "003-citysuv",
            "004-jungle", "004-junglectf", "011-seasidebase", "012-bluffstructure", "012-bluffstructurectf",
            "013-nightcity", "013-nightcity01", "016-themepark", "016-themeparkctf", "017-forest",
            "015-waterfall", "018-cave", "019-archlord", "024-city-marine", "025-fortress", "026-snowbase", "027-bubble"
        };

        private void OnGUI()
        {
            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
            EditorGUILayout.LabelField("Native map source", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            this.mapJsonPath = EditorGUILayout.TextField(this.mapJsonPath);
            if (GUILayout.Button("Browse", GUILayout.Width(72)))
            {
                var directory = File.Exists(this.mapJsonPath)
                    ? Path.GetDirectoryName(this.mapJsonPath)
                    : @"J:\dev\OpenGSNativeClient\assets\maps";
                var selected = EditorUtility.OpenFilePanel("Select native map JSON", directory, "json");
                if (!string.IsNullOrWhiteSpace(selected))
                {
                    this.mapJsonPath = selected;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Unity output", EditorStyles.boldLabel);
            this.outputRoot = EditorGUILayout.TextField("Generated folder", this.outputRoot);
            this.spriteSearchRoot = EditorGUILayout.TextField("Existing stage sprites", this.spriteSearchRoot);
            EditorGUILayout.HelpBox(
                "Builds a new preview scene and never overwrites an existing generated folder or production scene. " +
                "Missing textures are copied into the preview folder; existing project assets are reused without changing their import settings.",
                MessageType.Info);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Build isolated preview", GUILayout.Height(30)))
            {
                try
                {
                    this.lastBuiltScenePath = BuildPreview(this.mapJsonPath, this.outputRoot, this.spriteSearchRoot);
                    EditorUtility.DisplayDialog("Native map preview built", this.lastBuiltScenePath, "OK");
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorUtility.DisplayDialog("Native map preview failed", exception.Message, "OK");
                }
            }

            if (!string.IsNullOrWhiteSpace(this.lastBuiltScenePath) &&
                File.Exists(Path.GetFullPath(this.lastBuiltScenePath)))
            {
                if (GUILayout.Button("Open last preview additively"))
                {
                    EditorSceneManager.OpenScene(this.lastBuiltScenePath, OpenSceneMode.Additive);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public static string BuildPreview(
            string sourceMapJsonPath,
            string generatedAssetsRoot = DefaultOutputRoot,
            string existingSpriteSearchRoot = DefaultSpriteSearchRoot)
        {
            var absoluteMapPath = Path.GetFullPath(sourceMapJsonPath ?? string.Empty);
            if (!File.Exists(absoluteMapPath))
            {
                throw new FileNotFoundException("Native map JSON was not found.", absoluteMapPath);
            }

            var map = JObject.Parse(File.ReadAllText(absoluteMapPath));
            if (ReadInt(map["schemaVersion"], -1) != 1)
            {
                throw new InvalidDataException("Only native map schemaVersion 1 is supported.");
            }

            var mapId = map["id"]?.ToString();
            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new InvalidDataException("Native map JSON has no id.");
            }

            var spriteTokens = map["sprites"] as JArray;
            if (spriteTokens == null || spriteTokens.Count == 0)
            {
                throw new InvalidDataException("Native map JSON has no sprite placements.");
            }

            var bounds = map["bounds"] as JObject;
            if (bounds == null)
            {
                throw new InvalidDataException("Native map JSON has no bounds.");
            }

            var left = ReadFloat(bounds["left"], float.NaN);
            var bottom = ReadFloat(bounds["bottom"], float.NaN);
            var right = ReadFloat(bounds["right"], float.NaN);
            var top = ReadFloat(bounds["top"], float.NaN);
            var cameraWorldWidth = ReadFloat(map["cameraWorldWidth"], 8f);
            if (!IsFinite(left) || !IsFinite(bottom) || !IsFinite(right) || !IsFinite(top) ||
                !IsFinite(cameraWorldWidth) || cameraWorldWidth <= 0f ||
                right <= left || top <= bottom)
            {
                throw new InvalidDataException("Native map bounds are invalid.");
            }

            var outputRoot = NormalizeAssetFolder(generatedAssetsRoot);
            var spriteSearchRoot = NormalizeAssetFolder(existingSpriteSearchRoot);
            var safeMapId = SafeName(mapId);
            var mapOutputFolder = $"{outputRoot}/{safeMapId}";
            var sceneAssetPath = $"{mapOutputFolder}/{safeMapId}Preview.unity";
            var reportAssetPath = $"{mapOutputFolder}/{safeMapId}Preview.audit.json";

            if (AssetDatabase.IsValidFolder(mapOutputFolder) || File.Exists(Path.GetFullPath(mapOutputFolder)))
            {
                throw new IOException(
                    $"Output already exists: {mapOutputFolder}. Choose another generated folder or remove this preview first.");
            }

            if (File.Exists(Path.GetFullPath(sceneAssetPath)) || File.Exists(Path.GetFullPath(reportAssetPath)))
            {
                throw new IOException($"Preview output already exists under {mapOutputFolder}; nothing was overwritten.");
            }

            var sourceDirectory = Path.GetDirectoryName(absoluteMapPath);
            var plans = BuildSpritePlans(spriteTokens, sourceDirectory, spriteSearchRoot, mapOutputFolder);
            var obstacles = map["obstacles"] as JArray ?? new JArray();
            var polygons = map["polygons"] as JArray ?? new JArray();
            var groundColliders = map["groundColliders"] as JArray ?? new JArray();
            var offlineSpawn = map["offlineSpawn"] as JObject;
            ValidateColliders(obstacles, polygons);

            var outputFolderCreated = false;
            Scene previewScene = default;
            var previousActiveScene = SceneManager.GetActiveScene();
            try
            {
                EnsureAssetFolder(outputRoot);
                var parentFolder = Path.GetDirectoryName(mapOutputFolder).Replace('\\', '/');
                var folderName = Path.GetFileName(mapOutputFolder);
                if (AssetDatabase.CreateFolder(parentFolder, folderName).Length == 0 ||
                    !AssetDatabase.IsValidFolder(mapOutputFolder))
                {
                    throw new IOException($"Could not create generated folder {mapOutputFolder}.");
                }
                outputFolderCreated = true;

                var textureFolder = $"{mapOutputFolder}/Textures";
                if (plans.Any(plan => plan.CopyFromSource))
                {
                    if (AssetDatabase.CreateFolder(mapOutputFolder, "Textures").Length == 0 ||
                        !AssetDatabase.IsValidFolder(textureFolder))
                    {
                        throw new IOException($"Could not create generated texture folder {textureFolder}.");
                    }
                }

                foreach (var group in plans.Where(plan => plan.CopyFromSource).GroupBy(plan => plan.AssetPath))
                {
                    var plan = group.First();
                    if (!File.Exists(Path.GetFullPath(plan.AssetPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(plan.AssetPath)));
                        File.Copy(plan.SourcePath, Path.GetFullPath(plan.AssetPath), false);
                    }
                    ImportAsSprite(plan.AssetPath, group.ToList());
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var plan in plans)
                {
                    plan.Sprite = plan.TextureRect == null
                        ? LoadSingleSprite(plan.AssetPath)
                        : AssetDatabase.LoadAllAssetsAtPath(plan.AssetPath).OfType<Sprite>()
                            .SingleOrDefault(sprite => sprite.name == plan.Name);
                    if (plan.Sprite == null)
                    {
                        throw new InvalidDataException($"Could not load one Sprite from {plan.AssetPath}.");
                    }
                }

                var sceneMode = Application.isBatchMode && string.IsNullOrEmpty(previousActiveScene.path)
                    ? NewSceneMode.Single
                    : NewSceneMode.Additive;
                previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
                if (!previewScene.IsValid() || !previewScene.isLoaded)
                {
                    throw new InvalidOperationException("Could not create an isolated Unity preview scene.");
                }

                var root = new GameObject($"{safeMapId}_Preview");
                SceneManager.MoveGameObjectToScene(root, previewScene);
                var spriteRoot = new GameObject("Sprites");
                spriteRoot.transform.SetParent(root.transform, false);
                var collisionRoot = new GameObject("Collision");
                collisionRoot.transform.SetParent(root.transform, false);
                var groundRoot = new GameObject("Ground Surface");
                groundRoot.transform.SetParent(root.transform, false);
                var platformLayer = LayerMask.NameToLayer("Platforms");
                if (platformLayer < 0)
                {
                    throw new InvalidOperationException("Unity project has no 'Platforms' physics layer; generated map colliders cannot support PlayerAgent ground checks.");
                }
                collisionRoot.layer = platformLayer;
                groundRoot.layer = platformLayer;

                var spriteAudit = new JArray();
                foreach (var plan in plans)
                {
                    var spriteJson = plan.Json;
                    var sourceTransform = ReadAffineTransform(spriteJson["worldTransform"] as JArray, plan.Name);
                    var transform = sourceTransform;
                    ApplyNativeOverviewParallax(ref transform, spriteJson["parallax"] as JObject,
                        map, left, bottom, right, top, cameraWorldWidth);
                    var rendererObject = new GameObject($"{plan.Index:D4}_{SafeName(plan.Name)}");
                    rendererObject.transform.SetParent(spriteRoot.transform, false);
                    rendererObject.transform.localPosition = new Vector3(transform.X, transform.Y, 0f);
                    rendererObject.transform.localRotation = Quaternion.Euler(0f, 0f, transform.RotationDegrees);
                    var nativeSize = ReadVector2(spriteJson["size"], "size", plan.Name);
                    var spriteBounds = plan.Sprite.bounds.size;
                    if (spriteBounds.x <= 0f || spriteBounds.y <= 0f)
                    {
                        throw new InvalidDataException($"Sprite {plan.Name} has empty bounds.");
                    }
                    rendererObject.transform.localScale = new Vector3(
                        nativeSize.x / spriteBounds.x * transform.ScaleX,
                        nativeSize.y / spriteBounds.y * transform.ScaleY,
                        1f);

                    var renderer = rendererObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = plan.Sprite;
                    renderer.color = ReadColor(spriteJson["color"] as JObject);
                    renderer.flipX = ReadBool(spriteJson["flipX"], false);
                    renderer.flipY = ReadBool(spriteJson["flipY"], false) ^ transform.FlipY;
                    renderer.sortingLayerName = HasSortingLayer(spriteJson["sortingLayerName"]?.ToString())
                        ? spriteJson["sortingLayerName"].ToString()
                        : "Default";
                    var sourcePlacement = ReadInt(spriteJson["sourcePlacement"], plan.Index);
                    var sortingLayerOrder = ReadInt(spriteJson["sortingLayerOrder"], 0);
                    renderer.sortingOrder = sortingLayerOrder * 10000 + sourcePlacement;

                    spriteAudit.Add(new JObject
                    {
                        ["index"] = plan.Index,
                        ["sourcePlacement"] = sourcePlacement,
                        ["name"] = plan.Name,
                        ["sourcePath"] = plan.RelativeSourcePath,
                        ["unityAssetPath"] = plan.AssetPath,
                        ["copiedIntoPreview"] = plan.CopyFromSource,
                        ["sourceSha256"] = ComputeSha256(plan.SourcePath),
                        ["flipX"] = renderer.flipX,
                        ["flipY"] = renderer.flipY,
                        ["sortingOrder"] = renderer.sortingOrder,
                        ["position"] = new JArray(sourceTransform.X, sourceTransform.Y),
                        ["size"] = new JArray(nativeSize.x, nativeSize.y),
                        ["previewPosition"] = new JArray(transform.X, transform.Y),
                        ["previewScale"] = new JArray(transform.ScaleX, transform.ScaleY)
                    });
                }

                var boxColliderCount = 0;
                foreach (var token in obstacles.OfType<JObject>())
                {
                    var obstacle = ReadRect(token, "obstacle");
                    var collider = collisionRoot.AddComponent<BoxCollider2D>();
                    collider.offset = obstacle.Center;
                    collider.size = obstacle.Size;
                    boxColliderCount++;
                }

                var polygonColliderCount = 0;
                foreach (var token in polygons.OfType<JObject>())
                {
                    var paths = token["paths"] as JArray;
                    if (paths == null || paths.Count == 0)
                    {
                        continue;
                    }

                    var collider = collisionRoot.AddComponent<PolygonCollider2D>();
                    collider.pathCount = paths.Count;
                    for (var pathIndex = 0; pathIndex < paths.Count; pathIndex++)
                    {
                        var path = paths[pathIndex] as JArray;
                        if (path == null || path.Count < 3)
                        {
                            throw new InvalidDataException($"Polygon {polygonColliderCount} has a path with fewer than 3 points.");
                        }
                        collider.SetPath(pathIndex, path.Select(point => ReadPoint(point as JObject, "polygon point")).ToArray());
                    }
                    polygonColliderCount++;
                }

                var groundColliderCount = 0;
                foreach (var token in groundColliders.OfType<JObject>())
                {
                    var paths = token["paths"] as JArray;
                    if (paths == null || paths.Count == 0) continue;
                    var groundObject = new GameObject(token["name"]?.ToString() ?? $"Ground_{groundColliderCount:D3}");
                    groundObject.transform.SetParent(groundRoot.transform, false);
                    groundObject.layer = platformLayer;
                    var collider = groundObject.AddComponent<PolygonCollider2D>();
                    collider.pathCount = paths.Count;
                    for (var pathIndex = 0; pathIndex < paths.Count; pathIndex++)
                    {
                        var path = paths[pathIndex] as JArray;
                        if (path == null || path.Count < 3)
                            throw new InvalidDataException($"Ground collider {groundColliderCount} has a path with fewer than 3 points.");
                        collider.SetPath(pathIndex, path.Select(point => ReadPoint(point as JObject, "ground point")).ToArray());
                    }
                    groundColliderCount++;
                }

                if (offlineSpawn != null)
                {
                    var spawn = new GameObject("Offline Spawn");
                    spawn.transform.SetParent(root.transform, false);
                    spawn.transform.localPosition = new Vector3(
                        ReadFloat(offlineSpawn["x"], 0f), ReadFloat(offlineSpawn["y"], 0f), 0f);
                }

                CreatePreviewCamera(root.transform, left, bottom, right, top);
                if (!EditorSceneManager.SaveScene(previewScene, sceneAssetPath))
                {
                    throw new IOException($"Could not save generated scene {sceneAssetPath}.");
                }

                var report = new JObject
                {
                    ["schema"] = "OpenGS.UnityNativeMapPreview/1",
                    ["mapId"] = mapId,
                    ["sourceFile"] = Path.GetFileName(absoluteMapPath),
                    ["sourceJsonSha256"] = ComputeSha256(absoluteMapPath),
                    ["previewScene"] = sceneAssetPath,
                    ["spritePlacementCount"] = plans.Count,
                    ["uniqueSpriteFileCount"] = plans.Select(plan => plan.RelativeSourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    ["copiedSpriteFileCount"] = plans.Where(plan => plan.CopyFromSource)
                        .Select(plan => plan.RelativeSourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    ["obstacleBoxColliderCount"] = boxColliderCount,
                    ["polygonColliderCount"] = polygonColliderCount,
                    ["groundColliderCount"] = groundColliderCount,
                    ["collisionLayer"] = LayerMask.LayerToName(platformLayer),
                    ["previewAspect"] = NativeOverviewAspect,
                    ["offlineSpawn"] = offlineSpawn == null ? null : new JObject
                    {
                        ["x"] = ReadFloat(offlineSpawn["x"], 0f),
                        ["y"] = ReadFloat(offlineSpawn["y"], 0f)
                    },
                    ["bounds"] = new JObject
                    {
                        ["left"] = left,
                        ["bottom"] = bottom,
                        ["right"] = right,
                        ["top"] = top
                    },
                    ["sprites"] = spriteAudit
                };
                File.WriteAllText(Path.GetFullPath(reportAssetPath), report.ToString(Formatting.Indented), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(reportAssetPath, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();
                Debug.Log($"[NativeMapPreview] Built {mapId}: {plans.Count} sprite placements, " +
                          $"{boxColliderCount} box colliders, {polygonColliderCount} polygon colliders at {sceneAssetPath}");
                return sceneAssetPath;
            }
            catch
            {
                if (previewScene.IsValid() && previewScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(previewScene, true);
                }
                if (outputFolderCreated && AssetDatabase.IsValidFolder(mapOutputFolder))
                {
                    AssetDatabase.DeleteAsset(mapOutputFolder);
                    AssetDatabase.Refresh();
                }
                throw;
            }
            finally
            {
                if (previewScene.IsValid() && previewScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(previewScene, true);
                }
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded &&
                    SceneManager.GetActiveScene() != previousActiveScene)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }
        }

        private static List<SpritePlan> BuildSpritePlans(
            JArray spriteTokens,
            string sourceDirectory,
            string searchRoot,
            string mapOutputFolder)
        {
            var lookup = BuildSpriteLookup(searchRoot);
            var croppedSourcePaths = new HashSet<string>(spriteTokens.OfType<JObject>()
                .Where(token => token["textureRect"] != null && token["textureRect"].Type != JTokenType.Null)
                .Select(token => token["path"]?.ToString())
                .Where(path => !string.IsNullOrWhiteSpace(path)), StringComparer.OrdinalIgnoreCase);
            var planBySource = new Dictionary<string, TexturePlan>(StringComparer.OrdinalIgnoreCase);
            var plans = new List<SpritePlan>();

            for (var index = 0; index < spriteTokens.Count; index++)
            {
                var json = spriteTokens[index] as JObject;
                if (json == null)
                {
                    throw new InvalidDataException($"Sprite placement {index} is not an object.");
                }
                var name = json["name"]?.ToString() ?? $"Sprite_{index}";
                var textureRect = ReadTextureRect(json["textureRect"], name);

                var relativePath = json["path"]?.ToString();
                if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
                {
                    throw new InvalidDataException($"Sprite {name} has an invalid source path.");
                }
                var sourcePath = Path.GetFullPath(Path.Combine(sourceDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                var sourcePrefix = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!sourcePath.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Sprite {name} source path escapes the map directory.");
                }
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException($"Source texture for {name} was not found.", sourcePath);
                }

                var pivotObject = json["pivot"] as JObject;
                var pivot = new Vector2(ReadFloat(pivotObject?["x"], 0.5f), ReadFloat(pivotObject?["y"], 0.5f));
                if (pivot.x < 0f || pivot.x > 1f || pivot.y < 0f || pivot.y > 1f)
                {
                    throw new InvalidDataException($"Sprite {name} has a pivot outside the normalized range.");
                }

                if (!planBySource.TryGetValue(relativePath, out var texturePlan))
                {
                    var basename = Path.GetFileNameWithoutExtension(relativePath);
                    var candidates = lookup.TryGetValue(basename, out var found) ? found : new List<string>();
                    var reusablePath = !croppedSourcePaths.Contains(relativePath) &&
                                       candidates.Count == 1 && ExistingSpriteMatches(candidates[0], pivot)
                        ? candidates[0]
                        : null;
                    var assetPath = reusablePath ?? $"{mapOutputFolder}/Textures/{Path.GetFileName(relativePath)}";
                    texturePlan = new TexturePlan
                    {
                        RelativeSourcePath = relativePath,
                        SourcePath = sourcePath,
                        AssetPath = reusablePath ?? assetPath,
                        CopyFromSource = reusablePath == null,
                        Pivot = pivot
                    };
                    planBySource.Add(relativePath, texturePlan);
                }

                plans.Add(new SpritePlan
                {
                    Index = index,
                    Name = name,
                    Json = json,
                    RelativeSourcePath = texturePlan.RelativeSourcePath,
                    SourcePath = texturePlan.SourcePath,
                    AssetPath = texturePlan.AssetPath,
                    CopyFromSource = texturePlan.CopyFromSource,
                    Pivot = texturePlan.Pivot,
                    TextureRect = textureRect
                });
            }

            return plans;
        }

        private static Dictionary<string, List<string>> BuildSpriteLookup(string searchRoot)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(searchRoot))
            {
                return result;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { searchRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var basename = Path.GetFileNameWithoutExtension(assetPath);
                if (!result.TryGetValue(basename, out var paths))
                {
                    paths = new List<string>();
                    result.Add(basename, paths);
                }
                paths.Add(assetPath);
            }
            return result;
        }

        private static bool ExistingSpriteMatches(string assetPath, Vector2 expectedPivot)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
            if (sprites.Length != 1 || sprites[0].rect.width <= 0f || sprites[0].rect.height <= 0f)
            {
                return false;
            }
            var actualPivot = new Vector2(
                sprites[0].pivot.x / sprites[0].rect.width,
                sprites[0].pivot.y / sprites[0].rect.height);
            return Mathf.Abs(actualPivot.x - expectedPivot.x) < 0.001f &&
                   Mathf.Abs(actualPivot.y - expectedPivot.y) < 0.001f;
        }

        private static void ImportAsSprite(string assetPath, List<SpritePlan> placements)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidDataException($"Could not configure copied texture {assetPath}.");
            }
            importer.textureType = TextureImporterType.Sprite;
            var croppedPlacements = placements.Where(plan => plan.TextureRect != null)
                .GroupBy(plan => plan.Name, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            importer.spriteImportMode = croppedPlacements.Length > 0 ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            if (croppedPlacements.Length > 0)
            {
                importer.spritesheet = croppedPlacements.Select(plan => new SpriteMetaData
                {
                    name = plan.Name,
                    rect = plan.TextureRect.Value,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = plan.Pivot
                }).ToArray();
            }
            else
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = placements[0].Pivot;
                importer.SetTextureSettings(settings);
            }
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSingleSprite(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().SingleOrDefault();
        }

        private static void ValidateColliders(JArray obstacles, JArray polygons)
        {
            foreach (var token in obstacles.OfType<JObject>())
            {
                ReadRect(token, "obstacle");
            }
            foreach (var token in polygons.OfType<JObject>())
            {
                var paths = token["paths"] as JArray;
                if (paths == null)
                {
                    throw new InvalidDataException("Polygon group has no paths array.");
                }
                foreach (var path in paths.OfType<JArray>())
                {
                    if (path.Count < 3)
                    {
                        throw new InvalidDataException("Polygon paths must contain at least 3 points.");
                    }
                    foreach (var point in path)
                    {
                        ReadPoint(point as JObject, "polygon point");
                    }
                }
            }
        }

        private static void CreatePreviewCamera(Transform parent, float left, float bottom, float right, float top)
        {
            const float previewAspect = NativeOverviewAspect;
            var center = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
            var overviewWidth = Mathf.Max(right - left, (top - bottom) * previewAspect);
            var size = overviewWidth / (2f * previewAspect) * 1.02f;
            var cameraObject = new GameObject("Preview Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(center.x, center.y, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.aspect = previewAspect;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.14f, 0.17f, 1f);
        }

        private static void ApplyNativeOverviewParallax(
            ref AffineTransform transform, JObject parallax, JObject map,
            float left, float bottom, float right, float top, float cameraWorldWidth)
        {
            if (parallax == null)
            {
                return;
            }

            var factorX = ReadFloat(parallax["x"], 1f);
            var factorY = ReadFloat(parallax["y"], 1f);
            if (Mathf.Approximately(factorX, 1f) && Mathf.Approximately(factorY, 1f))
            {
                return;
            }

            var reference = parallax["reference"]?.ToString();
            var mapWidth = right - left;
            var mapHeight = top - bottom;
            if (string.Equals(reference, "map-top-left", StringComparison.Ordinal))
            {
                var centerX = (left + right) * 0.5f;
                var centerY = (bottom + top) * 0.5f;
                var viewWidth = Mathf.Max(mapWidth, mapHeight * NativeOverviewAspect);
                var viewHeight = viewWidth / NativeOverviewAspect;
                var backgroundWidth = Mathf.Min(cameraWorldWidth, mapWidth);
                var backgroundHeight = backgroundWidth / NativeOverviewAspect;
                if (backgroundHeight > mapHeight)
                {
                    backgroundHeight = mapHeight;
                    backgroundWidth = Mathf.Min(mapWidth, backgroundHeight * NativeOverviewAspect);
                }

                var viewLeft = centerX - viewWidth * 0.5f;
                var viewTop = centerY + viewHeight * 0.5f;
                var backgroundLeft = centerX - backgroundWidth * 0.5f;
                var backgroundTop = centerY + backgroundHeight * 0.5f;
                var overviewScale = viewWidth / backgroundWidth;
                transform.X = viewLeft + (transform.X - backgroundLeft) * overviewScale +
                    (viewLeft - left) * (1f - factorX) * overviewScale;
                transform.Y = viewTop + (transform.Y - backgroundTop) * overviewScale +
                    (viewTop - top) * (1f - factorY) * overviewScale;
                transform.ScaleX *= overviewScale;
                transform.ScaleY *= overviewScale;
                return;
            }

            if (!string.IsNullOrWhiteSpace(reference) && reference != "spawn")
            {
                throw new InvalidDataException($"Unsupported parallax reference '{reference}'.");
            }

            var spawn = map["spawn"] as JObject;
            var cameraX = (left + right) * 0.5f;
            var cameraY = (bottom + top) * 0.5f;
            transform.X += (ReadFloat(spawn?["x"], cameraX) - cameraX) * (1f - factorX);
            transform.Y += (ReadFloat(spawn?["y"], cameraY) - cameraY) * (1f - factorY);
        }

        private static AffineTransform ReadAffineTransform(JArray token, string name)
        {
            if (token == null || token.Count != 6)
            {
                throw new InvalidDataException($"Sprite {name} has an invalid worldTransform matrix.");
            }
            var m00 = ReadFloat(token[0], float.NaN);
            var m01 = ReadFloat(token[1], float.NaN);
            var x = ReadFloat(token[2], float.NaN);
            var m10 = ReadFloat(token[3], float.NaN);
            var m11 = ReadFloat(token[4], float.NaN);
            var y = ReadFloat(token[5], float.NaN);
            var scaleX = Mathf.Sqrt(m00 * m00 + m10 * m10);
            var columnYLength = Mathf.Sqrt(m01 * m01 + m11 * m11);
            var determinant = m00 * m11 - m01 * m10;
            if (!IsFinite(x) || !IsFinite(y) || scaleX <= 0f || columnYLength <= 0f ||
                Mathf.Abs(Vector2.Dot(new Vector2(m00, m10), new Vector2(m01, m11))) > scaleX * columnYLength * 0.001f ||
                Mathf.Abs(determinant) < 0.000001f)
            {
                throw new InvalidDataException($"Sprite {name} has a non-finite, sheared, or singular worldTransform.");
            }

            return new AffineTransform
            {
                X = x,
                Y = y,
                RotationDegrees = Mathf.Atan2(m10, m00) * Mathf.Rad2Deg,
                ScaleX = scaleX,
                ScaleY = Mathf.Abs(determinant) / scaleX,
                FlipY = determinant < 0f
            };
        }

        private static RectData ReadRect(JObject token, string label)
        {
            var left = ReadFloat(token["left"], float.NaN);
            var right = ReadFloat(token["right"], float.NaN);
            var bottom = ReadFloat(token["bottom"], float.NaN);
            var top = ReadFloat(token["top"], float.NaN);
            if (!IsFinite(left) || !IsFinite(right) || !IsFinite(bottom) || !IsFinite(top) ||
                right <= left || top <= bottom)
            {
                throw new InvalidDataException($"Invalid {label} rectangle.");
            }
            return new RectData
            {
                Center = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f),
                Size = new Vector2(right - left, top - bottom)
            };
        }

        private static Vector2 ReadVector2(JToken token, string field, string objectName)
        {
            if (!(token is JObject value))
            {
                throw new InvalidDataException($"Sprite {objectName} has no {field} vector.");
            }
            var x = ReadFloat(value["x"], float.NaN);
            var y = ReadFloat(value["y"], float.NaN);
            if (!IsFinite(x) || !IsFinite(y) || x <= 0f || y <= 0f)
            {
                throw new InvalidDataException($"Sprite {objectName} has an invalid {field} vector.");
            }
            return new Vector2(x, y);
        }

        private static Vector2 ReadPoint(JObject token, string label)
        {
            if (token == null)
            {
                throw new InvalidDataException($"Invalid {label}.");
            }
            var x = ReadFloat(token["x"], float.NaN);
            var y = ReadFloat(token["y"], float.NaN);
            if (!IsFinite(x) || !IsFinite(y))
            {
                throw new InvalidDataException($"Invalid {label} coordinates.");
            }
            return new Vector2(x, y);
        }

        private static Rect? ReadTextureRect(JToken token, string name)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }
            var value = token as JObject;
            var x = ReadFloat(value?["x"], float.NaN);
            var y = ReadFloat(value?["y"], float.NaN);
            var width = ReadFloat(value?["width"], float.NaN);
            var height = ReadFloat(value?["height"], float.NaN);
            if (!IsFinite(x) || !IsFinite(y) || !IsFinite(width) || !IsFinite(height) ||
                x < 0f || y < 0f || width <= 0f || height <= 0f)
            {
                throw new InvalidDataException($"Sprite {name} has an invalid textureRect.");
            }
            return new Rect(x, y, width, height);
        }

        private static Color ReadColor(JObject token)
        {
            return token == null
                ? Color.white
                : new Color(
                    ReadFloat(token["r"], 1f),
                    ReadFloat(token["g"], 1f),
                    ReadFloat(token["b"], 1f),
                    ReadFloat(token["a"], 1f));
        }

        private static bool HasSortingLayer(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && SortingLayer.layers.Any(layer => layer.name == name);
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }
            var parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
            var name = Path.GetFileName(assetFolder);
            EnsureAssetFolder(parent);
            if (AssetDatabase.CreateFolder(parent, name).Length == 0 && !AssetDatabase.IsValidFolder(assetFolder))
            {
                throw new IOException($"Could not create Unity folder {assetFolder}.");
            }
        }

        private static string NormalizeAssetFolder(string path)
        {
            var normalized = (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!normalized.Equals("Assets", StringComparison.OrdinalIgnoreCase) &&
                !normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Generated and sprite folders must be inside this Unity project's Assets folder.");
            }
            if (normalized.Split('/').Any(part => part == ".." || part == "."))
            {
                throw new InvalidDataException("Unity asset folder paths cannot contain dot segments.");
            }
            return normalized;
        }

        private static string SafeName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(character =>
                invalid.Contains(character) || character == '/' || character == '\\' ? '_' : character).ToArray());
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static float ReadFloat(JToken token, float fallback)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return fallback;
            }
            var value = token.Value<float>();
            return IsFinite(value) ? value : fallback;
        }

        private static int ReadInt(JToken token, int fallback)
        {
            return token == null || token.Type == JTokenType.Null ? fallback : token.Value<int>();
        }

        private static bool ReadBool(JToken token, bool fallback)
        {
            return token == null || token.Type == JTokenType.Null ? fallback : token.Value<bool>();
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private sealed class SpritePlan
        {
            public int Index;
            public string Name;
            public JObject Json;
            public string RelativeSourcePath;
            public string SourcePath;
            public string AssetPath;
            public bool CopyFromSource;
            public Vector2 Pivot;
            public Rect? TextureRect;
            public Sprite Sprite;
        }

        private sealed class TexturePlan
        {
            public string RelativeSourcePath;
            public string SourcePath;
            public string AssetPath;
            public bool CopyFromSource;
            public Vector2 Pivot;
        }

        private struct AffineTransform
        {
            public float X;
            public float Y;
            public float RotationDegrees;
            public float ScaleX;
            public float ScaleY;
            public bool FlipY;
        }

        private struct RectData
        {
            public Vector2 Center;
            public Vector2 Size;
        }
    }
}
#endif
