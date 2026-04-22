using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// ランキングUIクラス
    /// ランキング表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class RankingUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("ランキングリスト")]
        [SerializeField] private Transform rankingListContent;
        [SerializeField] private GameObject rankingItemPrefab;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("ランキング情報")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI playerRankText;
        [SerializeField] private TextMeshProUGUI playerHighScoreText;
        
        [Header("フィルター")]
        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private Button refreshButton;
        
        [Header("ボタン")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button clearButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject emptyMessage;

        // ─── 内部状態 ───────────────────────────────────────────────

        private List<RankingEntry> currentRanking = new List<RankingEntry>();
        private string currentGameMode = null;
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
            RefreshRanking();
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

            // タイトル
            if (titleText != null)
            {
                titleText.text = "ランキング";
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
        /// ランキングUIを表示する
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        public void Show(string playerName = "Player")
        {
            this.playerName = playerName;
            gameObject.SetActive(true);
            RefreshRanking();
        }

        /// <summary>
        /// プレイヤー名を設定する
        /// </summary>
        /// <param name="name">プレイヤー名</param>
        public void SetPlayerName(string name)
        {
            playerName = name;
            UpdatePlayerInfo();
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
            RefreshRanking();
        }

        private void OnRefreshButtonClicked()
        {
            RefreshRanking();
            ShowStatus("ランキングを更新しました", false);
        }

        private void OnCloseButtonClicked()
        {
            CloseDialog();
        }

        private void OnClearButtonClicked()
        {
            // 確認ダイアログを表示（実装は省略）
            RankingManager.Instance.ClearRanking(currentGameMode);
            RefreshRanking();
            ShowStatus("ランキングをクリアしました", false);
        }

        // ─── ランキング更新 ─────────────────────────────────────────

        /// <summary>
        /// ランキングを更新する
        /// </summary>
        private void RefreshRanking()
        {
            // ランキングデータを取得
            currentRanking = RankingManager.Instance.GetTopRanking(10, currentGameMode);

            // リストをクリア
            ClearRankingList();

            // ランキングアイテムを生成
            if (currentRanking.Count == 0)
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

                for (int i = 0; i < currentRanking.Count; i++)
                {
                    CreateRankingItem(i + 1, currentRanking[i]);
                }
            }

            // プレイヤー情報を更新
            UpdatePlayerInfo();
        }

        /// <summary>
        /// ランキングリストをクリアする
        /// </summary>
        private void ClearRankingList()
        {
            if (rankingListContent == null) return;

            foreach (Transform child in rankingListContent)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// ランキングアイテムを生成する
        /// </summary>
        private void CreateRankingItem(int rank, RankingEntry entry)
        {
            if (rankingItemPrefab == null || rankingListContent == null) return;

            var item = Instantiate(rankingItemPrefab, rankingListContent);
            var itemScript = item.GetComponent<RankingItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(rank, entry, playerName);
            }
            else
            {
                // フォールバック：直接UIを設定
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"{rank}位";
                    texts[1].text = entry.PlayerName;
                    texts[2].text = entry.Score.ToString();
                }
            }
        }

        /// <summary>
        /// プレイヤー情報を更新する
        /// </summary>
        private void UpdatePlayerInfo()
        {
            // プレイヤーの順位
            if (playerRankText != null)
            {
                int rank = RankingManager.Instance.GetPlayerRank(playerName, currentGameMode);
                playerRankText.text = rank > 0 ? $"順位: {rank}位" : "順位: 圏外";
            }

            // プレイヤーの最高スコア
            if (playerHighScoreText != null)
            {
                int highScore = RankingManager.Instance.GetPlayerHighScore(playerName, currentGameMode);
                playerHighScoreText.text = $"最高スコア: {highScore}";
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