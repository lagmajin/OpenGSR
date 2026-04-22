using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// マッチ履歴UIクラス
    /// マッチ履歴の表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class MatchHistoryUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("履歴リスト")]
        [SerializeField] private Transform historyListContent;
        [SerializeField] private GameObject historyItemPrefab;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("履歴情報")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI totalMatchesText;
        [SerializeField] private TextMeshProUGUI winRateText;
        
        [Header("フィルター")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_Dropdown resultDropdown;
        [SerializeField] private Button refreshButton;
        
        [Header("ボタン")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button clearButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject emptyMessage;

        // ─── 内部状態 ───────────────────────────────────────────────

        private List<MatchHistoryEntry> currentHistory = new List<MatchHistoryEntry>();
        private string currentGameMode = null;
        private string currentResult = null;
        private string playerName = "Player";

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
            RefreshHistory();
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // ゲームモードドロップダウンの初期化
            if (gameModeDropdown != null)
            {
                gameModeDropdown.ClearOptions();
                var options = new List<string> { "全モード", "DeathMatch", "TeamDeathMatch", "CaptureTheFlag", "Survival" };
                gameModeDropdown.AddOptions(options);
                gameModeDropdown.value = 0;
            }

            // 結果ドロップダウンの初期化
            if (resultDropdown != null)
            {
                resultDropdown.ClearOptions();
                var options = new List<string> { "全て", "勝利", "敗北", "引き分け" };
                resultDropdown.AddOptions(options);
                resultDropdown.value = 0;
            }

            // タイトル
            if (titleText != null)
            {
                titleText.text = "マッチ履歴";
            }

            // エラーテキストをクリア
            if (statusText != null)
            {
                statusText.text = "";
                statusText.gameObject.SetActive(false);
            }

            // 空メッセージを非表示
            if (emptyMessage != null)
            {
                emptyMessage.SetActive(false);
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            if (gameModeDropdown != null)
            {
                gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
            }

            if (resultDropdown != null)
            {
                resultDropdown.onValueChanged.AddListener(OnResultChanged);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(OnRefreshButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClearButtonClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// マッチ履歴UIを表示する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        public void Show(string playerName = "Player")
        {
            this.playerName = playerName;
            gameObject.SetActive(true);
            RefreshHistory();
        }

        /// <summary>
        /// プレイヤー名を設定する
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        public void SetPlayerName(string name)
        {
            playerName = name;
            UpdateStatistics();
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnGameModeChanged(int index)
        {
            switch (index)
            {
                case 0: currentGameMode = null; break;
                case 1: currentGameMode = "DeathMatch"; break;
                case 2: currentGameMode = "TeamDeathMatch"; break;
                case 3: currentGameMode = "CaptureTheFlag"; break;
                case 4: currentGameMode = "Survival"; break;
            }
            RefreshHistory();
        }

        private void OnResultChanged(int index)
        {
            switch (index)
            {
                case 0: currentResult = null; break;
                case 1: currentResult = "Win"; break;
                case 2: currentResult = "Lose"; break;
                case 3: currentResult = "Draw"; break;
            }
            RefreshHistory();
        }

        private void OnRefreshButtonClicked()
        {
            RefreshHistory();
            ShowStatus("履歴を更新しました", false);
        }

        private void OnCloseButtonClicked()
        {
            CloseDialog();
        }

        private void OnClearButtonClicked()
        {
            // 確認ダイアログを表示（実装は省略）
            MatchHistoryManager.Instance.ClearHistory(playerName);
            RefreshHistory();
            ShowStatus("履歴をクリアしました", false);
        }

        // ─── 履歴更新 ─────────────────────────────────────────────

        /// <summary>
        /// 履歴を更新する
        /// </summary>
        private void RefreshHistory()
        {
            // 履歴データを取得
            var allHistory = MatchHistoryManager.Instance.GetPlayerHistory(playerName, 50);

            // フィルタリング
            currentHistory = allHistory;

            if (!string.IsNullOrEmpty(currentGameMode))
            {
                currentHistory = currentHistory.FindAll(e => e.GameMode == currentGameMode);
            }

            if (!string.IsNullOrEmpty(currentResult))
            {
                currentHistory = currentHistory.FindAll(e => e.Result == currentResult);
            }

            // リストをクリア
            ClearHistoryList();

            // 履歴アイテムを生成
            if (currentHistory.Count == 0)
            {
                if (emptyMessage != null)
                {
                    emptyMessage.SetActive(true);
                }
            }
            else
            {
                if (emptyMessage != null)
                {
                    emptyMessage.SetActive(false);
                }

                foreach (var entry in currentHistory)
                {
                    CreateHistoryItem(entry);
                }
            }

            // 統計情報を更新
            UpdateStatistics();
        }

        /// <summary>
        /// 履歴リストをクリアする
        /// </summary>
        private void ClearHistoryList()
        {
            if (historyListContent == null) return;

            foreach (Transform child in historyListContent)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 履歴アイテムを生成する
        /// </summary>
        private void CreateHistoryItem(MatchHistoryEntry entry)
        {
            if (historyItemPrefab == null || historyListContent == null) return;

            var item = Instantiate(historyItemPrefab, historyListContent);
            var itemScript = item.GetComponent<MatchHistoryItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(entry);
            }
            else
            {
                // フォールバック：直接UIを設定
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4)
                {
                    texts[0].text = entry.GameMode;
                    texts[1].text = entry.Result;
                    texts[2].text = entry.Score.ToString();
                    texts[3].text = entry.Timestamp;
                }
            }
        }

        /// <summary>
        /// 統計情報を更新する
        /// </summary>
        private void UpdateStatistics()
        {
            var stats = MatchHistoryManager.Instance.GetStatistics(playerName, currentGameMode);

            // 総試合数
            if (totalMatchesText != null)
            {
                totalMatchesText.text = $"総試合数: {stats.TotalMatches}";
            }

            // 勝率
            if (winRateText != null)
            {
                winRateText.text = $"勝率: {stats.WinRate:F1}%";
            }
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