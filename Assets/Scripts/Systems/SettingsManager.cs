using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenGS
{
    /// <summary>
    /// 設定マネージャー
    /// ゲーム設定の管理と永続化を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // ─── シングルトン ───────────────────────────────────────────

        private static SettingsManager _instance;
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SettingsManager");
                    _instance = go.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ─── 定数 ─────────────────────────────────────────────────

        private const string SETTINGS_SAVE_KEY = "GameSettings";

        // ─── 内部状態 ───────────────────────────────────────────────

        private GameSettings settings = new GameSettings();
        private bool isInitialized = false;

        // ─── イベント ───────────────────────────────────────────────

        public event Action<GameSettings> OnSettingsChanged;
        public event Action<GraphicsSettings> OnGraphicsSettingsChanged;
        public event Action<SoundSettings> OnSoundSettingsChanged;
        public event Action<ControlSettings> OnControlSettingsChanged;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// 設定システムを初期化する
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            LoadSettings();
            isInitialized = true;

            Debug.Log("[SettingsManager] 初期化完了");
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 設定を取得する
        /// </summary>
        /// <returns>ゲーム設定</returns>
        public GameSettings GetSettings()
        {
            return settings;
        }

        /// <summary>
        /// グラフィックス設定を取得する
        /// </summary>
        /// <returns>グラフィックス設定</returns>
        public GraphicsSettings GetGraphicsSettings()
        {
            return settings.Graphics;
        }

        /// <summary>
        /// サウンド設定を取得する
        /// </summary>
        /// <returns>サウンド設定</returns>
        public SoundSettings GetSoundSettings()
        {
            return settings.Sound;
        }

        /// <summary>
        /// 操作設定を取得する
        /// </summary>
        /// <returns>操作設定</returns>
        public ControlSettings GetControlSettings()
        {
            return settings.Control;
        }

        /// <summary>
        /// グラフィックス設定を適用する
        /// </summary>
        /// <param name="graphicsSettings">グラフィックス設定</param>
        public void ApplyGraphicsSettings(GraphicsSettings graphicsSettings)
        {
            settings.Graphics = graphicsSettings;
            ApplyGraphicsSettings();
            SaveSettings();

            OnGraphicsSettingsChanged?.Invoke(graphicsSettings);
            OnSettingsChanged?.Invoke(settings);

            Debug.Log("[SettingsManager] グラフィックス設定を適用しました");
        }

        /// <summary>
        /// サウンド設定を適用する
        /// </summary>
        /// <param name="soundSettings">サウンド設定</param>
        public void ApplySoundSettings(SoundSettings soundSettings)
        {
            settings.Sound = soundSettings;
            ApplySoundSettings();
            SaveSettings();

            OnSoundSettingsChanged?.Invoke(soundSettings);
            OnSettingsChanged?.Invoke(settings);

            Debug.Log("[SettingsManager] サウンド設定を適用しました");
        }

        /// <summary>
        /// 操作設定を適用する
        /// </summary>
        /// <param name="controlSettings">操作設定</param>
        public void ApplyControlSettings(ControlSettings controlSettings)
        {
            settings.Control = controlSettings;
            SaveSettings();

            OnControlSettingsChanged?.Invoke(controlSettings);
            OnSettingsChanged?.Invoke(settings);

            Debug.Log("[SettingsManager] 操作設定を適用しました");
        }

        /// <summary>
        /// 設定をリセットする
        /// </summary>
        public void ResetSettings()
        {
            settings = new GameSettings();
            ApplyAllSettings();
            SaveSettings();

            OnSettingsChanged?.Invoke(settings);

            Debug.Log("[SettingsManager] 設定をリセットしました");
        }

        /// <summary>
        /// 設定をエクスポートする
        /// </summary>
        /// <returns>JSON形式の設定データ</returns>
        public string ExportSettings()
        {
            return JsonConvert.SerializeObject(settings, Formatting.Indented);
        }

        /// <summary>
        /// 設定をインポートする
        /// </summary>
        /// <param name="json">JSON形式の設定データ</param>
        public void ImportSettings(string json)
        {
            try
            {
                var importedSettings = JsonConvert.DeserializeObject<GameSettings>(json);
                if (importedSettings != null)
                {
                    settings = importedSettings;
                    ApplyAllSettings();
                    SaveSettings();

                    OnSettingsChanged?.Invoke(settings);
                    Debug.Log("[SettingsManager] 設定をインポートしました");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsManager] インポートエラー: {ex.Message}");
            }
        }

        // ─── プライベートメソッド ─────────────────────────────────────

        /// <summary>
        /// 設定を読み込む
        /// </summary>
        private void LoadSettings()
        {
            var json = PlayerPrefs.GetString(SETTINGS_SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<GameSettings>(json) ?? new GameSettings();
                    Debug.Log("[SettingsManager] 設定を読み込みました");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SettingsManager] 読み込みエラー: {ex.Message}");
                    settings = new GameSettings();
                }
            }
            else
            {
                settings = new GameSettings();
            }

            ApplyAllSettings();
        }

        /// <summary>
        /// 設定を保存する
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings);
                PlayerPrefs.SetString(SETTINGS_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SettingsManager] 設定を保存しました");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsManager] 保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 全設定を適用する
        /// </summary>
        private void ApplyAllSettings()
        {
            ApplyGraphicsSettings();
            ApplySoundSettings();
        }

        /// <summary>
        /// グラフィックス設定を適用する
        /// </summary>
        private void ApplyGraphicsSettings()
        {
            // 解像度設定
            Screen.SetResolution(
                settings.Graphics.ResolutionWidth,
                settings.Graphics.ResolutionHeight,
                settings.Graphics.Fullscreen
            );

            // 品質設定
            QualitySettings.SetQualityLevel(settings.Graphics.QualityLevel);

            // VSync設定
            QualitySettings.vSyncCount = settings.Graphics.VSync ? 1 : 0;

            // フレームレート設定
            Application.targetFrameRate = settings.Graphics.TargetFrameRate;

            Debug.Log($"[SettingsManager] グラフィックス設定を適用: {settings.Graphics.ResolutionWidth}x{settings.Graphics.ResolutionHeight}, Quality:{settings.Graphics.QualityLevel}");
        }

        /// <summary>
        /// サウンド設定を適用する
        /// </summary>
        private void ApplySoundSettings()
        {
            // マスターボリューム
            AudioListener.volume = settings.Sound.MasterVolume;
            TryApplyReverb(settings.Sound.Reverb);

            Debug.Log($"[SettingsManager] サウンド設定を適用: MasterVolume={settings.Sound.MasterVolume}");
        }

        private void TryApplyReverb(bool enabled)
        {
            try
            {
                OpenGSR.Audio.SimpleAudioManager.Instance.SetReverbEnabled(enabled);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SettingsManager] リバーブ適用をスキップ: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ゲーム設定クラス
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public GraphicsSettings Graphics = new GraphicsSettings();
        public SoundSettings Sound = new SoundSettings();
        public ControlSettings Control = new ControlSettings();
    }

    /// <summary>
    /// グラフィックス設定クラス
    /// </summary>
    [Serializable]
    public class GraphicsSettings
    {
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public bool Fullscreen = true;
        public int QualityLevel = 2; // 0:Low, 1:Medium, 2:High, 3:Ultra
        public bool VSync = true;
        public int TargetFrameRate = 60;
        public float Brightness = 1.0f;
        public bool AntiAliasing = true;
        public bool Shadows = true;
        public int ShadowQuality = 2; // 0:Low, 1:Medium, 2:High
    }

    /// <summary>
    /// サウンド設定クラス
    /// </summary>
    [Serializable]
    public class SoundSettings
    {
        public float MasterVolume = 1.0f;
        public float BGMVolume = 0.8f;
        public float SEVolume = 1.0f;
        public float VoiceVolume = 1.0f;
        public bool MuteAll = false;
        public bool Reverb = false;
    }

    /// <summary>
    /// 操作設定クラス
    /// </summary>
    [Serializable]
    public class ControlSettings
    {
        public float MouseSensitivity = 1.0f;
        public bool InvertMouseY = false;
        public bool AutoAim = true;
        public Dictionary<string, string> KeyBindings = new Dictionary<string, string>();
    }
}
