using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OpenGS
{
    /// <summary>
    /// 設定画面UIクラス
    /// ゲーム設定の表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("タブ")]
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button soundTabButton;
        [SerializeField] private Button controlTabButton;
        [SerializeField] private GameObject graphicsTabHighlight;
        [SerializeField] private GameObject soundTabHighlight;
        [SerializeField] private GameObject controlTabHighlight;
        
        [Header("設定パネル")]
        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject soundPanel;
        [SerializeField] private GameObject controlPanel;
        
        [Header("ボタン")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;

        // ─── 内部状態 ───────────────────────────────────────────────

        private SettingsTab currentTab = SettingsTab.Graphics;
        private bool hasUnsavedChanges = false;

        // ─── 列挙型 ─────────────────────────────────────────────────

        private enum SettingsTab
        {
            Graphics,
            Sound,
            Control
        }

        // ─── デリゲート ─────────────────────────────────────────────

        public Action OnDialogClosed;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            InitializeUI();
            SetupListeners();
        }

        private void OnEnable()
        {
            ShowTab(currentTab);
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // エラーテキストをクリア
            if (statusText != null)
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            // タブボタン
            if (graphicsTabButton != null)
            {
                graphicsTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Graphics));
            }

            if (soundTabButton != null)
            {
                soundTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Sound));
            }

            if (controlTabButton != null)
            {
                controlTabButton.onClick.AddListener(() => ShowTab(SettingsTab.Control));
            }

            // 操作ボタン
            if (applyButton != null)
            {
                applyButton.onClick.AddListener(OnApplyButtonClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 設定画面を表示する
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            ShowTab(currentTab);
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnApplyButtonClicked()
        {
            ApplySettings();
            ShowStatus("設定を適用しました", false);
        }

        private void OnResetButtonClicked()
        {
            SettingsManager.Instance.ResetSettings();
            ShowTab(currentTab);
            ShowStatus("設定をリセットしました", false);
        }

        private void OnCloseButtonClicked()
        {
            if (hasUnsavedChanges)
            {
                // 確認ダイアログを表示（実装は省略）
                ApplySettings();
            }

            CloseDialog();
        }

        // ─── タブ管理 ───────────────────────────────────────────────

        /// <summary>
        /// タブを表示する
        /// </summary>
        private void ShowTab(SettingsTab tab)
        {
            currentTab = tab;

            // タブハイライトを更新
            UpdateTabHighlight(tab);

            // パネルを表示/非表示
            if (graphicsPanel != null)
            {
                graphicsPanel.SetActive(tab == SettingsTab.Graphics);
            }

            if (soundPanel != null)
            {
                soundPanel.SetActive(tab == SettingsTab.Sound);
            }

            if (controlPanel != null)
            {
                controlPanel.SetActive(tab == SettingsTab.Control);
            }
        }

        /// <summary>
        /// タブハイライトを更新する
        /// </summary>
        private void UpdateTabHighlight(SettingsTab tab)
        {
            if (graphicsTabHighlight != null)
            {
                graphicsTabHighlight.SetActive(tab == SettingsTab.Graphics);
            }

            if (soundTabHighlight != null)
            {
                soundTabHighlight.SetActive(tab == SettingsTab.Sound);
            }

            if (controlTabHighlight != null)
            {
                controlTabHighlight.SetActive(tab == SettingsTab.Control);
            }
        }

        // ─── 設定適用 ───────────────────────────────────────────────

        /// <summary>
        /// 設定を適用する
        /// </summary>
        private void ApplySettings()
        {
            // 各パネルから設定を取得して適用
            var graphicsPanelScript = graphicsPanel?.GetComponent<GraphicsSettingsUI>();
            if (graphicsPanelScript != null)
            {
                graphicsPanelScript.ApplySettings();
            }

            var soundPanelScript = soundPanel?.GetComponent<SoundSettingsUI>();
            if (soundPanelScript != null)
            {
                soundPanelScript.ApplySettings();
            }

            var controlPanelScript = controlPanel?.GetComponent<ControlSettingsUI>();
            if (controlPanelScript != null)
            {
                controlPanelScript.ApplySettings();
            }

            hasUnsavedChanges = false;
        }

        // ─── ステータス表示 ─────────────────────────────────────────

        /// <summary>
        /// ステータスメッセージを表示する
        /// </summary>
        private void ShowStatus(string message, bool isError)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = isError ? Color.red : Color.green;
                statusText.gameObject.SetActive(true);

                // 3秒後に非表示
                CancelInvoke(nameof(HideStatus));
                Invoke(nameof(HideStatus), 3f);
            }
        }

        /// <summary>
        /// ステータスメッセージを非表示にする
        /// </summary>
        private void HideStatus()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
        }

        // ─── ダイアログ制御 ─────────────────────────────────────────

        /// <summary>
        /// ダイアログを閉じる
        /// </summary>
        private void CloseDialog()
        {
            gameObject.SetActive(false);
            OnDialogClosed?.Invoke();
        }
    }
}