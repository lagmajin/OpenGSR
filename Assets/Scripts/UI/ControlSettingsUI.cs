using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// 操作設定UIクラス
    /// 操作設定の表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class ControlSettingsUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("マウス設定")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TextMeshProUGUI mouseSensitivityText;
        [SerializeField] private Toggle invertMouseYToggle;
        [SerializeField] private Toggle autoAimToggle;
        
        [Header("キーバインド")]
        [SerializeField] private Transform keyBindListContent;
        [SerializeField] private GameObject keyBindItemPrefab;
        
        [Header("プリセット")]
        [SerializeField] private Button defaultPresetButton;
        [SerializeField] private Button fpsPresetButton;
        [SerializeField] private Button tpsPresetButton;
        
        [Header("リセット")]
        [SerializeField] private Button resetKeyBindingsButton;

        // ─── 内部状態 ───────────────────────────────────────────────

        private ControlSettings currentSettings;
        private Dictionary<string, string> keyBindings = new Dictionary<string, string>();
        private List<KeyBindItem> keyBindItems = new List<KeyBindItem>();

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
            // マウス感度スライダーの初期化
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = 0.1f;
                mouseSensitivitySlider.maxValue = 5.0f;
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            // マウス設定
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            }

            if (invertMouseYToggle != null)
            {
                invertMouseYToggle.onValueChanged.AddListener(OnInvertMouseYChanged);
            }

            if (autoAimToggle != null)
            {
                autoAimToggle.onValueChanged.AddListener(OnAutoAimChanged);
            }

            // プリセット
            if (defaultPresetButton != null)
            {
                defaultPresetButton.onClick.AddListener(OnDefaultPresetClicked);
            }

            if (fpsPresetButton != null)
            {
                fpsPresetButton.onClick.AddListener(OnFPSPresetClicked);
            }

            if (tpsPresetButton != null)
            {
                tpsPresetButton.onClick.AddListener(OnTPSPresetClicked);
            }

            // リセット
            if (resetKeyBindingsButton != null)
            {
                resetKeyBindingsButton.onClick.AddListener(OnResetKeyBindingsClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 設定を適用する
        /// </summary>
        public void ApplySettings()
        {
            currentSettings.KeyBindings = new Dictionary<string, string>(keyBindings);
            SettingsManager.Instance.ApplyControlSettings(currentSettings);
            Debug.Log("[ControlSettingsUI] 操作設定を適用しました");
        }

        // ─── 設定読み込み ─────────────────────────────────────────

        /// <summary>
        /// 現在の設定を読み込む
        /// </summary>
        private void LoadCurrentSettings()
        {
            currentSettings = SettingsManager.Instance.GetControlSettings();
            keyBindings = new Dictionary<string, string>(currentSettings.KeyBindings);
            UpdateUI();
            RefreshKeyBindList();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            // マウス設定
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = currentSettings.MouseSensitivity;
            }

            if (mouseSensitivityText != null)
            {
                mouseSensitivityText.text = $"{currentSettings.MouseSensitivity:F1}";
            }

            if (invertMouseYToggle != null)
            {
                invertMouseYToggle.isOn = currentSettings.InvertMouseY;
            }

            if (autoAimToggle != null)
            {
                autoAimToggle.isOn = currentSettings.AutoAim;
            }
        }

        /// <summary>
        /// キーバインドリストを更新する
        /// </summary>
        private void RefreshKeyBindList()
        {
            // リストをクリア
            ClearKeyBindList();

            // デフォルトのキーバインド
            var defaultKeyBinds = new Dictionary<string, string>
            {
                { "MoveForward", "W" },
                { "MoveBackward", "S" },
                { "MoveLeft", "A" },
                { "MoveRight", "D" },
                { "Jump", "Space" },
                { "Crouch", "LeftControl" },
                { "Sprint", "LeftShift" },
                { "Fire", "Mouse0" },
                { "Aim", "Mouse1" },
                { "Reload", "R" },
                { "Interact", "E" },
                { "Inventory", "Tab" },
                { "Map", "M" },
                { "Scoreboard", "Tab" },
                { "Chat", "Enter" }
            };

            // キーバインドアイテムを生成
            foreach (var kvp in defaultKeyBinds)
            {
                string action = kvp.Key;
                string key = keyBindings.ContainsKey(action) ? keyBindings[action] : kvp.Value;
                CreateKeyBindItem(action, key);
            }
        }

        /// <summary>
        /// キーバインドリストをクリアする
        /// </summary>
        private void ClearKeyBindList()
        {
            if (keyBindListContent == null) return;

            foreach (Transform child in keyBindListContent)
            {
                Destroy(child.gameObject);
            }
            keyBindItems.Clear();
        }

        /// <summary>
        /// キーバインドアイテムを生成する
        /// </summary>
        private void CreateKeyBindItem(string action, string key)
        {
            if (keyBindItemPrefab == null || keyBindListContent == null) return;

            var item = Instantiate(keyBindItemPrefab, keyBindListContent);
            var itemScript = item.GetComponent<KeyBindItem>();

            if (itemScript != null)
            {
                itemScript.Setup(action, key, OnKeyBindChanged);
                keyBindItems.Add(itemScript);
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnMouseSensitivityChanged(float value)
        {
            currentSettings.MouseSensitivity = value;

            if (mouseSensitivityText != null)
            {
                mouseSensitivityText.text = $"{value:F1}";
            }
        }

        private void OnInvertMouseYChanged(bool isOn)
        {
            currentSettings.InvertMouseY = isOn;
        }

        private void OnAutoAimChanged(bool isOn)
        {
            currentSettings.AutoAim = isOn;
        }

        private void OnKeyBindChanged(string action, string newKey)
        {
            keyBindings[action] = newKey;
        }

        private void OnDefaultPresetClicked()
        {
            ApplyPreset("Default");
        }

        private void OnFPSPresetClicked()
        {
            ApplyPreset("FPS");
        }

        private void OnTPSPresetClicked()
        {
            ApplyPreset("TPS");
        }

        private void OnResetKeyBindingsClicked()
        {
            keyBindings.Clear();
            RefreshKeyBindList();
            Debug.Log("[ControlSettingsUI] キーバインドをリセットしました");
        }

        // ─── プリセット適用 ─────────────────────────────────────────

        /// <summary>
        /// プリセットを適用する
        /// </summary>
        private void ApplyPreset(string presetName)
        {
            switch (presetName)
            {
                case "Default":
                    currentSettings.MouseSensitivity = 1.0f;
                    currentSettings.InvertMouseY = false;
                    currentSettings.AutoAim = true;
                    break;

                case "FPS":
                    currentSettings.MouseSensitivity = 1.5f;
                    currentSettings.InvertMouseY = false;
                    currentSettings.AutoAim = false;
                    break;

                case "TPS":
                    currentSettings.MouseSensitivity = 1.2f;
                    currentSettings.InvertMouseY = false;
                    currentSettings.AutoAim = true;
                    break;
            }

            UpdateUI();
            Debug.Log($"[ControlSettingsUI] プリセットを適用しました: {presetName}");
        }
    }
}