//using SFB;
using System.Collections.Generic;
//using System.Windows.Forms;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using System.IO;
using System;


#pragma warning disable 0414

namespace OpenGS
{
    public enum EExport
    {
        All,
        BGM,
        BGN
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameTimer))]
    public class ExportAssetScene : MonoBehaviour
    {
        //export directories (relative to project root or Assets as appropriate)
        private string exportDirectory = "Asset";
        private string stagesSpriteDirectory = "Stages";
        private string playersSpriteDirectory = "Players";
        private string effectsSpriteDirectory = "Effects";
        private string itemsSpriteDirectory = "Items";
        private string weaponSpriteDirectory = "Weapons";
        private string uiSpriteDirectory = "Uis";
        private string bgmSpriteDirectory = "Bgm";
        private string bgnSpriteDirectory = "Bgn";
        private string soundDirectory = "Sound";

        [SerializeField] private GameTimer timer;

        public GameObject SoundStorageManager;

        [SceneObjectsOnly] public GameObject spritesRootGameObject;

        void Start()
        {
            // Avoid performing heavy I/O automatically on Start to minimize side effects.
            Debug.Log("ExportAssetScene ready. Use the inspector buttons or call ExportAllAssets()/ExportBgm() to run exports.");
        }

        [Button("自動セット")]
        public void AutoSet()
        {
            // noop for now - placeholder for editor helper logic
            Debug.Log("AutoSet called (not implemented)");
        }

        [Button("Export All Assets")]
        public void ExportAllAssets()
        {
            ExportAssetFiles();
        }

        [Button("Export Stage Sprites")]
        public void ExportStageSpritesButton()
        {
            ExportsStageSprite();
        }

        [Button("Export Stage BGM")]
        public void ExportStageBgmButton()
        {
            ExportsStageBgm();
        }

        void OpenSelectFileDialog()
        {
            // kept for future implementation
        }

        private void WriteFile(Sprite sp)
        {
            if (sp == null)
            {
                Debug.LogWarning("WriteFile: sprite is null");
                return;
            }

            var tex = sp.texture;
            if (tex == null)
            {
                Debug.LogWarning($"Sprite '{sp.name}' has no texture");
                return;
            }

            try
            {
                // Ensure texture is readable. If not, we cannot encode.
                if (!(tex is Texture2D tex2d))
                {
                    Debug.LogWarning($"Texture for sprite '{sp.name}' is not Texture2D");
                    return;
                }

                // When sprites use atlases, the sprite rect may be used to extract pixels.
                var rect = sp.rect;
                Texture2D extracted;

                if (rect.width != tex2d.width || rect.height != tex2d.height)
                {
                    // Extract sprite rectangle to new texture
                    extracted = new Texture2D((int)rect.width, (int)rect.height);
                    try
                    {
                        var pixels = tex2d.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                        extracted.SetPixels(pixels);
                        extracted.Apply();
                    }
                    catch (UnityException)
                    {
                        Debug.LogWarning($"Texture for sprite '{sp.name}' is not readable. Enable Read/Write in import settings.");
                        return;
                    }
                }
                else
                {
                    extracted = tex2d;
                }

                var png = extracted.EncodeToPNG();
                var exportPath = Path.Combine(Application.dataPath, exportDirectory, stagesSpriteDirectory);
                if (!Directory.Exists(exportPath)) Directory.CreateDirectory(exportPath);

                var filePath = Path.Combine(exportPath, sp.name + ".png");
                File.WriteAllBytes(filePath, png);
                Debug.Log($"Wrote sprite to {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write sprite '{sp.name}': {e.Message}");
            }
        }

        private void WriteFile()
        {
            // placeholder overload
        }

        private List<Sprite> GetSprites(GameObject obj)
        {
            var result = new List<Sprite>();
            if (obj == null) return result;

            // Collect SpriteRenderer sprites in children
            var srs = obj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (sr.sprite != null && !result.Contains(sr.sprite)) result.Add(sr.sprite);
            }

            // Also check Image components (UI) when in editor only
#if UNITY_EDITOR
            var uiImages = obj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in uiImages)
            {
                if (img.sprite != null && !result.Contains(img.sprite)) result.Add(img.sprite);
            }
#endif

            return result;
        }

        private void ExportsStageSprite()
        {
            if (spritesRootGameObject == null)
            {
                Debug.LogWarning("spritesRootGameObject not assigned");
                return;
            }

            Debug.Log("Exporting sprites from root: " + spritesRootGameObject.name);
            var sprites = GetSprites(spritesRootGameObject);
            Debug.Log($"Found {sprites.Count} sprites to export");
            foreach (var sp in sprites)
            {
                WriteFile(sp);
            }
        }

        private void ExportsStageBgm()
        {
            // "Audio/BGM" フォルダからすべての AudioClip をロード
            AudioClip[] allBgm = Resources.LoadAll<AudioClip>("Audio/BGM");

            // ロードしたBGMファイルの数をログに表示
            Debug.Log($"Loaded {allBgm.Length} BGM tracks.");

            // BGMファイルを順に保存
            foreach (AudioClip bgm in allBgm)
            {
                string directory = Path.Combine(Application.dataPath, "ExportedBGM");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string filePath = Path.Combine(directory, bgm.name + ".wav");

                // SaveWavメソッドを呼び出して、WAVファイルとして保存
                SaveWav(filePath, bgm);

                Debug.Log($"Saved BGM: {bgm.name} to {filePath}");
            }
        }

        private void ExportsStageBgn()
        {
            Debug.Log("Export BGN not implemented");
        }

        private void ExportPlayerSprite()
        {
            Debug.Log("Exports player sprite");
        }


        private void ExportAssetFiles()
        {
            Debug.Log("Asset exporting started...");

            var appDatapath = Application.dataPath;

            // fixed path concatenation bug - do not duplicate dataPath
            var spritePath = Path.Combine(appDatapath, exportDirectory, stagesSpriteDirectory) + Path.DirectorySeparatorChar;

            Debug.Log("sprite export path: " + spritePath);

            var soundPath = Path.Combine(appDatapath, soundDirectory);
            Debug.Log("sound path: " + soundPath);

            // For now only export bgm to avoid unintended heavy operations
            ExportsStageBgm();
            // Provide additional runtime extraction utilities for emergency recovery
            Debug.Log("Exporting runtime textures and audio for recovery...");
            ExportRuntimeTextures();
            ExportRuntimeAudioClips();
        }

        [Button("Export Runtime Textures")]
        public void ExportRuntimeTextures()
        {
            try
            {
                var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                Debug.Log($"Found {textures.Length} runtime Texture2D objects.");
                var outDir = Path.Combine(Application.dataPath, "ExportedRuntime", "Textures");
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                foreach (var t in textures)
                {
                    if (t == null) continue;
                    try
                    {
                        Texture2D readable = null;
                        try
                        {
                            // If texture is readable, this will succeed
                            var _ = t.GetPixels(0);
                            readable = t;
                        }
                        catch (UnityException)
                        {
                            // Not readable: create readable copy via RenderTexture
                            readable = MakeTextureReadable(t);
                        }

                        if (readable == null)
                        {
                            Debug.LogWarning($"Could not obtain readable texture for {t.name}");
                            continue;
                        }

                        byte[] png = readable.EncodeToPNG();
                        var filePath = Path.Combine(outDir, t.name + ".png");
                        File.WriteAllBytes(filePath, png);
                        Debug.Log($"Exported runtime texture: {filePath}");

                        // If we created a temporary readable copy, destroy it to avoid leaks
                        if (!ReferenceEquals(readable, t))
                        {
                            UnityEngine.Object.DestroyImmediate(readable);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed exporting texture {t.name}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ExportRuntimeTextures failed: {e.Message}");
            }
        }

        private Texture2D MakeTextureReadable(Texture2D source)
        {
            if (source == null) return null;
            try
            {
                var width = source.width;
                var height = source.height;
                var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                var prev = RenderTexture.active;
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                return tex;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MakeTextureReadable failed for {source.name}: {e.Message}");
                return null;
            }
        }

        [Button("Export Runtime AudioClips")]
        public void ExportRuntimeAudioClips()
        {
            try
            {
                var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
                Debug.Log($"Found {clips.Length} runtime AudioClip objects.");
                var outDir = Path.Combine(Application.dataPath, "ExportedRuntime", "Audio");
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                foreach (var c in clips)
                {
                    if (c == null) continue;
                    try
                    {
                        var filePath = Path.Combine(outDir, c.name + ".wav");
                        SaveWav(filePath, c);
                        Debug.Log($"Exported audio clip: {filePath}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed exporting audio {c.name}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ExportRuntimeAudioClips failed: {e.Message}");
            }
        }

        private void path()
        {

        }
        public static void SaveWav(string filePath, AudioClip audioClip)
        {
            // サンプルの数とチャネル数を計算
            float[] samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);

            try
            {
                // ファイルを作成
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    // WAV ヘッダーの書き込み
                    writer.Write(0x46464952); // "RIFF" in ASCII
                    writer.Write((int)(36 + samples.Length * 2)); // ファイルサイズ
                    writer.Write(0x45564157); // "WAVE" in ASCII
                    writer.Write(0x20746D66); // "fmt " in ASCII
                    writer.Write(16); // Sub-chunk size
                    writer.Write((short)1); // Audio format (1 for PCM)
                    writer.Write((short)audioClip.channels); // チャネル数
                    writer.Write(audioClip.frequency); // サンプルレート
                    writer.Write(audioClip.frequency * audioClip.channels * 2); // バイトレート
                    writer.Write((short)(audioClip.channels * 2)); // ブロックアライメント
                    writer.Write((short)16); // サンプルのビット数
                    writer.Write(0x61746164); // "data" in ASCII
                    writer.Write(samples.Length * 2); // データ長

                    // オーディオデータの書き込み
                    foreach (float sample in samples)
                    {
                        // -1.0から1.0の範囲のサンプル値を16ビット整数に変換
                        writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * 32767.0f));
                    }
                }
                Debug.Log($"WAV file saved successfully at {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving WAV file: {e.Message}");
            }
        }
    }

}
