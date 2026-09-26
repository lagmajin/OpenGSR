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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
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
            NormalizeSettings();
            return settings;
        }

        /// <summary>
        /// グラフィックス設定を取得する
        /// </summary>
        /// <returns>グラフィックス設定</returns>
        public GraphicsSettings GetGraphicsSettings()
        {
            NormalizeSettings();
            return settings.Graphics;
        }

        /// <summary>
        /// サウンド設定を取得する
        /// </summary>
        /// <returns>サウンド設定</returns>
        public SoundSettings GetSoundSettings()
        {
            NormalizeSettings();
            return settings.Sound;
        }

        /// <summary>
        /// 操作設定を取得する
        /// </summary>
        /// <returns>操作設定</returns>
        public ControlSettings GetControlSettings()
        {
            NormalizeSettings();
            return settings.Control;
        }

        public GameplaySettings GetGameplaySettings()
        {
            NormalizeSettings();
            return settings.Gameplay;
        }

        /// <summary>
        /// グラフィックス設定を適用する
        /// </summary>
        /// <param name="graphicsSettings">グラフィックス設定</param>
        public void ApplyGraphicsSettings(GraphicsSettings graphicsSettings)
        {
            settings.Graphics = graphicsSettings ?? new GraphicsSettings();
            NormalizeSettings();
            ApplyGraphicsSettings();
            SaveSettings();

            InvokeSafely(OnGraphicsSettingsChanged, settings.Graphics, nameof(OnGraphicsSettingsChanged));
            InvokeSafely(OnSettingsChanged, settings, nameof(OnSettingsChanged));

            Debug.Log("[SettingsManager] グラフィックス設定を適用しました");
        }

        /// <summary>
        /// サウンド設定を適用する
        /// </summary>
        /// <param name="soundSettings">サウンド設定</param>
        public void ApplySoundSettings(SoundSettings soundSettings)
        {
            settings.Sound = soundSettings ?? new SoundSettings();
            NormalizeSettings();
            ApplySoundSettings();
            SaveSettings();

            InvokeSafely(OnSoundSettingsChanged, settings.Sound, nameof(OnSoundSettingsChanged));
            InvokeSafely(OnSettingsChanged, settings, nameof(OnSettingsChanged));

            Debug.Log("[SettingsManager] サウンド設定を適用しました");
        }

        /// <summary>
        /// 操作設定を適用する
        /// </summary>
        /// <param name="controlSettings">操作設定</param>
        public void ApplyControlSettings(ControlSettings controlSettings)
        {
            settings.Control = controlSettings ?? new ControlSettings();
            NormalizeSettings();
            SaveSettings();

            InvokeSafely(OnControlSettingsChanged, settings.Control, nameof(OnControlSettingsChanged));
            InvokeSafely(OnSettingsChanged, settings, nameof(OnSettingsChanged));

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

            InvokeSafely(OnGraphicsSettingsChanged, settings.Graphics, nameof(OnGraphicsSettingsChanged));
            InvokeSafely(OnSoundSettingsChanged, settings.Sound, nameof(OnSoundSettingsChanged));
            InvokeSafely(OnControlSettingsChanged, settings.Control, nameof(OnControlSettingsChanged));
            InvokeSafely(OnSettingsChanged, settings, nameof(OnSettingsChanged));

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
                    NormalizeSettings();
                    ApplyAllSettings();
                    SaveSettings();

            InvokeSafely(OnGraphicsSettingsChanged, settings.Graphics, nameof(OnGraphicsSettingsChanged));
            InvokeSafely(OnSoundSettingsChanged, settings.Sound, nameof(OnSoundSettingsChanged));
            InvokeSafely(OnControlSettingsChanged, settings.Control, nameof(OnControlSettingsChanged));
            InvokeSafely(OnSettingsChanged, settings, nameof(OnSettingsChanged));
                    Debug.Log("[SettingsManager] 設定をインポートしました");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SettingsManager] インポートエラー: {ex.Message}");
            }
        }

        private static void InvokeSafely<T>(Action<T> handlers, T value, string eventName)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SettingsManager] {eventName} subscriber failed: {ex}");
                }
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
                    NormalizeSettings();
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

        private void NormalizeSettings()
        {
            settings ??= new GameSettings();
            settings.Graphics ??= new GraphicsSettings();
            settings.Sound ??= new SoundSettings();
            settings.Control ??= new ControlSettings();
            settings.Gameplay ??= new GameplaySettings();
            settings.Control.KeyBindings ??= new Dictionary<string, string>();

            var normalizedBindings = new Dictionary<string, string>();
            foreach (var pair in settings.Control.KeyBindings)
            {
                var action = pair.Key?.Trim();
                var binding = pair.Value?.Trim();
                if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(binding))
                {
                    continue;
                }

                if (!normalizedBindings.ContainsKey(action))
                {
                    normalizedBindings[action] = binding;
                }
            }

            settings.Control.KeyBindings = normalizedBindings;

            // Tab is a hold-to-show scoreboard key.  Older defaults assigned
            // it to Inventory as well, which could drop the equipped weapon
            // whenever the scoreboard was opened.  Migrate only that exact
            // conflict and preserve all other user bindings.
            if (settings.Control.KeyBindings.TryGetValue("Inventory", out var inventoryBinding) &&
                settings.Control.KeyBindings.TryGetValue("Scoreboard", out var scoreboardBinding) &&
                string.Equals(inventoryBinding, "Tab", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(scoreboardBinding, "Tab", StringComparison.OrdinalIgnoreCase))
            {
                settings.Control.KeyBindings["Inventory"] = "G";
            }

            settings.Graphics.ResolutionWidth = Mathf.Max(320, settings.Graphics.ResolutionWidth);
            settings.Graphics.ResolutionHeight = Mathf.Max(240, settings.Graphics.ResolutionHeight);
            settings.Graphics.QualityLevel = Mathf.Clamp(settings.Graphics.QualityLevel, 0, 3);
            settings.Graphics.TargetFrameRate = settings.Graphics.TargetFrameRate == -1
                ? -1
                : Mathf.Clamp(settings.Graphics.TargetFrameRate, 15, 240);
            settings.Graphics.Brightness = NormalizeFinite01(settings.Graphics.Brightness, 1f);
            settings.Graphics.ShadowQuality = Mathf.Clamp(settings.Graphics.ShadowQuality, 0, 2);

            settings.Sound.MasterVolume = NormalizeFinite01(settings.Sound.MasterVolume, 1f);
            settings.Sound.BGMVolume = NormalizeFinite01(settings.Sound.BGMVolume, 0.8f);
            settings.Sound.SEVolume = NormalizeFinite01(settings.Sound.SEVolume, 1f);
            settings.Sound.VoiceVolume = NormalizeFinite01(settings.Sound.VoiceVolume, 1f);
            settings.Control.MouseSensitivity = NormalizeFiniteMin(settings.Control.MouseSensitivity, 0.01f, 1f);
            settings.Gameplay.RespawnDelaySeconds = NormalizeFiniteMin(settings.Gameplay.RespawnDelaySeconds, 0f, 5f);
        }

        private static float NormalizeFinite01(float value, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Clamp01(value) : fallback;
        }

        private static float NormalizeFiniteMin(float value, float minimum, float fallback)
        {
            return float.IsFinite(value) ? Mathf.Max(minimum, value) : fallback;
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
            NormalizeSettings();
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

            // 詳細な描画設定
            QualitySettings.antiAliasing = settings.Graphics.AntiAliasing
                ? settings.Graphics.QualityLevel switch
                {
                    0 => 2,
                    1 => 2,
                    2 => 4,
                    _ => 8
                }
                : 0;
            QualitySettings.shadows = settings.Graphics.Shadows
                ? ShadowQuality.All
                : ShadowQuality.Disable;
            QualitySettings.shadowResolution = settings.Graphics.ShadowQuality switch
            {
                0 => ShadowResolution.Low,
                1 => ShadowResolution.Medium,
                _ => ShadowResolution.High
            };

            // フレームレート設定
            Application.targetFrameRate = settings.Graphics.TargetFrameRate;

            Debug.Log($"[SettingsManager] グラフィックス設定を適用: {settings.Graphics.ResolutionWidth}x{settings.Graphics.ResolutionHeight}, Quality:{settings.Graphics.QualityLevel}");
        }

        /// <summary>
        /// サウンド設定を適用する
        /// </summary>
        private void ApplySoundSettings()
        {
            NormalizeSettings();
            // マスターボリュームと個別ミキサー音量を実際の再生系へ反映する。
            // MuteAll は保存値を壊さず、再生時の有効音量だけを 0 にする。
            var sound = settings.Sound;
            var muteMultiplier = sound.MuteAll ? 0f : 1f;
            AudioListener.volume = sound.MasterVolume * muteMultiplier;

            try
            {
                var audioManager = OpenGSR.Audio.SimpleAudioManager.Instance;
                audioManager.SetBGMVolume(sound.BGMVolume * muteMultiplier);
                audioManager.SetSEVolume(sound.SEVolume * muteMultiplier);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SettingsManager] BGM/SE音量適用をスキップ: {ex.Message}");
            }

            TryApplyReverb(settings.Sound.Reverb);

            Debug.Log($"[SettingsManager] サウンド設定を適用: MasterVolume={sound.MasterVolume}, BGM={sound.BGMVolume}, SE={sound.SEVolume}, MuteAll={sound.MuteAll}");
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
        public GameplaySettings Gameplay = new GameplaySettings();
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

    /// <summary>
    /// ゲームプレイ設定クラス
    /// </summary>
    [Serializable]
    public class GameplaySettings
    {
        public float RespawnDelaySeconds = 5.0f;
    }
}
