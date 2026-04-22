using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// グラフィックス設定UIクラス
    /// グラフィックス設定の表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class GraphicsSettingsUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("解像度設定")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        
        [Header("品質設定")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown frameRateDropdown;
        
        [Header("詳細設定")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private TextMeshProUGUI brightnessValueText;
        [SerializeField] private Toggle antiAliasingToggle;
        [SerializeField] private Toggle shadowsToggle;
        [SerializeField] private TMP_Dropdown shadowQualityDropdown;
        
        [Header("プレビュー")]
        [SerializeField] private Image previewImage;

        // ─── 内部状態 ───────────────────────────────────────────────

        private GraphicsSettings currentSettings;
        private List<Resolution> availableResolutions = new List<Resolution>();

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            InitializeUI();
            SetupListeners();
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // 解像度ドロップダウンの初期化
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                availableResolutions = new List<Resolution>(Screen.resolutions);
                var resolutionOptions = new List<string>();

                foreach (var res in availableResolutions)
                {
                    resolutionOptions.Add($"{res.width} x {res.height}");
                }

                resolutionDropdown.AddOptions(resolutionOptions);
            }

            // 品質ドロップダウンの初期化
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                var qualityOptions = new List<string> { "Low", "Medium", "High", "Ultra" };
                qualityDropdown.AddOptions(qualityOptions);
            }

            // フレームレートドロップダウンの初期化
            if (frameRateDropdown != null)
            {
                frameRateDropdown.ClearOptions();
                var frameRateOptions = new List<string> { "30", "60", "120", "144", "Unlimited" };
                frameRateDropdown.AddOptions(frameRateOptions);
            }

            // シャドウ品質ドロップダウンの初期化
            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.ClearOptions();
                var shadowOptions = new List<string> { "Low", "Medium", "High" };
                shadowQualityDropdown.AddOptions(shadowOptions);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            // 解像度設定
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            // 品質設定
            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            }

            if (frameRateDropdown != null)
            {
                frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
            }

            // 詳細設定
            if (brightnessSlider != null)
            {
                brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            }

            if (antiAliasingToggle != null)
            {
                antiAliasingToggle.onValueChanged.AddListener(OnAntiAliasingChanged);
            }

            if (shadowsToggle != null)
            {
                shadowsToggle.onValueChanged.AddListener(OnShadowsChanged);
            }

            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 設定を適用する
        /// </summary>
        public void ApplySettings()
        {
            SettingsManager.Instance.ApplyGraphicsSettings(currentSettings);
            Debug.Log("[GraphicsSettingsUI] グラフィックス設定を適用しました");
        }

        // ─── 設定読み込み ─────────────────────────────────────────

        /// <summary>
        /// 現在の設定を読み込む
        /// </summary>
        private void LoadCurrentSettings()
        {
            currentSettings = SettingsManager.Instance.GetGraphicsSettings();
            UpdateUI();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            // 解像度設定
            if (resolutionDropdown != null)
            {
                var currentRes = availableResolutions.FindIndex(r =>
                    r.width == currentSettings.ResolutionWidth &&
                    r.height == currentSettings.ResolutionHeight);

                if (currentRes >= 0)
                {
                    resolutionDropdown.value = currentRes;
                }
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = currentSettings.Fullscreen;
            }

            // 品質設定
            if (qualityDropdown != null)
            {
                qualityDropdown.value = currentSettings.QualityLevel;
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.isOn = currentSettings.VSync;
            }

            if (frameRateDropdown != null)
            {
                int frameRateIndex = currentSettings.TargetFrameRate switch
                {
                    30 => 0,
                    60 => 1,
                    120 => 2,
                    144 => 3,
                    _ => 4 // Unlimited
                };
                frameRateDropdown.value = frameRateIndex;
            }

            // 詳細設定
            if (brightnessSlider != null)
            {
                brightnessSlider.value = currentSettings.Brightness;
            }

            if (brightnessValueText != null)
            {
                brightnessValueText.text = $"{currentSettings.Brightness:F2}";
            }

            if (antiAliasingToggle != null)
            {
                antiAliasingToggle.isOn = currentSettings.AntiAliasing;
            }

            if (shadowsToggle != null)
            {
                shadowsToggle.isOn = currentSettings.Shadows;
            }

            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.value = currentSettings.ShadowQuality;
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnResolutionChanged(int index)
        {
            if (index >= 0 && index < availableResolutions.Count)
            {
                var resolution = availableResolutions[index];
                currentSettings.ResolutionWidth = resolution.width;
                currentSettings.ResolutionHeight = resolution.height;
            }
        }

        private void OnFullscreenChanged(bool isOn)
        {
            currentSettings.Fullscreen = isOn;
        }

        private void OnQualityChanged(int index)
        {
            currentSettings.QualityLevel = index;
        }

        private void OnVSyncChanged(bool isOn)
        {
            currentSettings.VSync = isOn;
        }

        private void OnFrameRateChanged(int index)
        {
            currentSettings.TargetFrameRate = index switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                3 => 144,
                _ => -1 // Unlimited
            };
        }

        private void OnBrightnessChanged(float value)
        {
            currentSettings.Brightness = value;

            if (brightnessValueText != null)
            {
                brightnessValueText.text = $"{value:F2}";
            }
        }

        private void OnAntiAliasingChanged(bool isOn)
        {
            currentSettings.AntiAliasing = isOn;
        }

        private void OnShadowsChanged(bool isOn)
        {
            currentSettings.Shadows = isOn;
        }

        private void OnShadowQualityChanged(int index)
        {
            currentSettings.ShadowQuality = index;
        }
    }
}