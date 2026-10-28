using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenGS;

namespace OpenGS.EditorTools
{
    public static class RecoveredMapPlaytestBuilder
    {
        private const string SourceMap = @"J:\dev\OpenGSNativeClient\assets\maps\local-original-015-waterfall.json";
        private const string OutputRoot = "Assets/GeneratedMapPlaytests";
        private const string SpriteSearchRoot = "Assets/Sprites/Stages";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/MistyPlayable.prefab";

        [MenuItem("Tools/OpenGS/Build Waterfall Playtest Scene")]
        public static void BuildWaterfall()
        {
            var scenePath = NativeMapPreviewBuilderWindow.BuildPreview(
                SourceMap, OutputRoot, SpriteSearchRoot, nativeOverview: false);
            ConfigureAndOpen(scenePath);
        }

        [MenuItem("Tools/OpenGS/Rebuild Waterfall Playtest Scene")]
        public static void RebuildWaterfall()
        {
            var map = JObject.Parse(File.ReadAllText(SourceMap));
            var folder = $"{OutputRoot}/{SafeFolderName(map["id"]?.ToString())}";
            if (AssetDatabase.IsValidFolder(folder) && !AssetDatabase.DeleteAsset(folder))
            {
                throw new IOException($"Could not remove generated playtest folder {folder}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildWaterfall();
        }

        private static void ConfigureAndOpen(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var map = JObject.Parse(File.ReadAllText(SourceMap));
            var spawn = map["offlineSpawn"] as JObject;
            if (spawn == null)
            {
                throw new InvalidDataException("Recovered Waterfall map has no offlineSpawn.");
            }

            var spawnPosition = new Vector2((float)spawn["x"], (float)spawn["y"]);
            var camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);
            if (camera == null)
            {
                throw new InvalidDataException("Generated recovered map has no camera.");
            }

            camera.tag = "MainCamera";
            camera.orthographicSize = map["cameraWorldWidth"] != null
                ? (float)map["cameraWorldWidth"] / (2f * (16f / 9f))
                : 2.25f;
            camera.transform.position = new Vector3(spawnPosition.x, spawnPosition.y + 0.3f, -10f);
            if (Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include) == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
            var follow = camera.GetComponent<RecoveredMapCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<RecoveredMapCameraFollow>();
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null || playerPrefab.GetComponent<PlayerAgent>() == null)
            {
                throw new InvalidDataException($"Playable player prefab is missing or has no PlayerAgent: {PlayerPrefabPath}");
            }

            var bootstrapObject = new GameObject("Recovered Map Playtest");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            bootstrapObject.AddComponent<RecoveredMapPlaytestBootstrap>()
                .Configure(playerPrefab, spawnPosition, camera);

            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new IOException($"Could not save generated playtest scene {scenePath}.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[RecoveredMapPlaytest] Built {scenePath}. The game player spawns at {spawnPosition} and uses the restored Platforms colliders.");
        }

        private static string SafeFolderName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Recovered map JSON has no id.");
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }
    }
}
