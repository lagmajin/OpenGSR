using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// フレンドリストUIクラス
    /// フレンドリストの表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class FriendListUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("フレンドリスト")]
        [SerializeField] private Transform friendListContent;
        [SerializeField] private GameObject friendItemPrefab;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("フレンド情報")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI friendCountText;
        [SerializeField] private TextMeshProUGUI onlineCountText;
        
        [Header("フィルター")]
        [SerializeField] private Toggle onlineOnlyToggle;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        
        [Header("ボタン")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button addFriendButton;
        [SerializeField] private Button pendingRequestsButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject emptyMessage;

        // ─── 内部状態 ───────────────────────────────────────────────

        private List<FriendEntry> currentFriends = new List<FriendEntry>();
        private bool showOnlineOnly = false;
        private string searchKeyword = "";

        // ─── デリゲート ─────────────────────────────────────────────

        public Action OnDialogClosed;
        public Action OnAddFriendClicked;
        public Action OnPendingRequestsClicked;

        // ─── Unity ライフサイクル ────────────────────────────────────

        private void Awake()
        {
            InitializeUI();
            SetupListeners();
        }

        private void OnEnable()
        {
            RefreshFriendList();
        }

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// UI要素を初期化する
        /// </summary>
        private void InitializeUI()
        {
            // タイトル
            if (titleText != null)
            {
                titleText.text = "フレンドリスト";
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
            if (onlineOnlyToggle != null)
            {
                onlineOnlyToggle.onValueChanged.AddListener(OnOnlineOnlyToggleChanged);
            }

            if (searchButton != null)
            {
                searchButton.onClick.AddListener(OnSearchButtonClicked);
            }

            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchValueChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (addFriendButton != null)
            {
                addFriendButton.onClick.AddListener(OnAddFriendButtonClicked);
            }

            if (pendingRequestsButton != null)
            {
                pendingRequestsButton.onClick.AddListener(OnPendingRequestsButtonClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// フレンドリストUIを表示する
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            RefreshFriendList();
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnOnlineOnlyToggleChanged(bool isOn)
        {
            showOnlineOnly = isOn;
            RefreshFriendList();
        }

        private void OnSearchButtonClicked()
        {
            FilterFriends(searchInput.text);
        }

        private void OnSearchValueChanged(string value)
        {
            searchKeyword = value;
            FilterFriends(value);
        }

        private void OnCloseButtonClicked()
        {
            CloseDialog();
        }

        private void OnAddFriendButtonClicked()
        {
            OnAddFriendClicked?.Invoke();
        }

        private void OnPendingRequestsButtonClicked()
        {
            OnPendingRequestsClicked?.Invoke();
        }

        // ─── フレンドリスト更新 ─────────────────────────────────────

        /// <summary>
        /// フレンドリストを更新する
        /// </summary>
        private void RefreshFriendList()
        {
            // フレンドデータを取得
            var allFriends = FriendManager.Instance.GetFriends(showOnlineOnly);

            // 検索フィルタリング
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                allFriends = allFriends.FindAll(f => 
                    f.PlayerName.IndexOf(searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            currentFriends = allFriends;

            // リストをクリア
            ClearFriendList();

            // フレンドアイテムを生成
            if (currentFriends.Count == 0)
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

                foreach (var friend in currentFriends)
                {
                    CreateFriendItem(friend);
                }
            }

            // 統計情報を更新
            UpdateStatistics();
        }

        /// <summary>
        /// フレンドリストをクリアする
        /// </summary>
        private void ClearFriendList()
        {
            if (friendListContent == null) return;

            foreach (Transform child in friendListContent)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// フレンドアイテムを生成する
        /// </summary>
        private void CreateFriendItem(FriendEntry friend)
        {
            if (friendItemPrefab == null || friendListContent == null) return;

            var item = Instantiate(friendItemPrefab, friendListContent);
            var itemScript = item.GetComponent<FriendItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(friend, OnFriendItemSelected);
            }
            else
            {
                // フォールバック：直接UIを設定
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = friend.PlayerName;
                    texts[1].text = friend.IsOnline ? "オンライン" : "オフライン";
                }
            }
        }

        /// <summary>
        /// フレンドアイテムが選択されたときの処理
        /// </summary>
        private void OnFriendItemSelected(FriendEntry friend)
        {
            Debug.Log($"[FriendListUI] フレンドを選択しました: {friend.PlayerName}");
            // 詳細表示やアクションメニューを表示する処理を追加可能
        }

        /// <summary>
        /// フレンドをフィルタリングする
        /// </summary>
        private void FilterFriends(string keyword)
        {
            searchKeyword = keyword;
            RefreshFriendList();
        }

        /// <summary>
        /// 統計情報を更新する
        /// </summary>
        private void UpdateStatistics()
        {
            var allFriends = FriendManager.Instance.GetFriends();
            var onlineFriends = FriendManager.Instance.GetFriends(true);

            // 総フレンド数
            if (friendCountText != null)
            {
                friendCountText.text = $"フレンド: {allFriends.Count}人";
            }

            // オンライン数
            if (onlineCountText != null)
            {
                onlineCountText.text = $"オンライン: {onlineFriends.Count}人";
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