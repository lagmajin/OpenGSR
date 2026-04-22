using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace OpenGS
{
    /// <summary>
    /// フレンド申請UIクラス
    /// フレンド申請の表示と操作を提供
    /// メインコードに接続なしで独立して動作
    /// </summary>
    public class FriendRequestUI : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [Header("申請リスト")]
        [SerializeField] private Transform requestListContent;
        [SerializeField] private GameObject requestItemPrefab;
        [SerializeField] private ScrollRect scrollRect;
        
        [Header("申請情報")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI requestCountText;
        
        [Header("ボタン")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button refreshButton;
        
        [Header("エラー/ステータス")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject emptyMessage;

        // ─── 内部状態 ───────────────────────────────────────────────

        private List<FriendRequest> currentRequests = new List<FriendRequest>();

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
            RefreshRequestList();
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
                titleText.text = "フレンド申請";
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
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(OnRefreshButtonClicked);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// フレンド申請UIを表示する
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            RefreshRequestList();
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnCloseButtonClicked()
        {
            CloseDialog();
        }

        private void OnRefreshButtonClicked()
        {
            RefreshRequestList();
            ShowStatus("申請リストを更新しました", false);
        }

        // ─── 申請リスト更新 ─────────────────────────────────────────

        /// <summary>
        /// 申請リストを更新する
        /// </summary>
        private void RefreshRequestList()
        {
            // 申請データを取得
            currentRequests = FriendManager.Instance.GetPendingRequests();

            // リストをクリア
            ClearRequestList();

            // 申請アイテムを生成
            if (currentRequests.Count == 0)
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

                foreach (var request in currentRequests)
                {
                    CreateRequestItem(request);
                }
            }

            // 統計情報を更新
            UpdateStatistics();
        }

        /// <summary>
        /// 申請リストをクリアする
        /// </summary>
        private void ClearRequestList()
        {
            if (requestListContent == null) return;

            foreach (Transform child in requestListContent)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 申請アイテムを生成する
        /// </summary>
        private void CreateRequestItem(FriendRequest request)
        {
            if (requestItemPrefab == null || requestListContent == null) return;

            var item = Instantiate(requestItemPrefab, requestListContent);
            var itemScript = item.GetComponent<FriendRequestItem>();
            
            if (itemScript != null)
            {
                itemScript.Setup(request, OnRequestAccepted, OnRequestRejected);
            }
            else
            {
                // フォールバック：直接UIを設定
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = request.SenderPlayerName;
                    texts[1].text = request.RequestDate;
                }
            }
        }

        /// <summary>
        /// 申請が承認されたときの処理
        /// </summary>
        private void OnRequestAccepted(FriendRequest request)
        {
            FriendManager.Instance.AcceptFriendRequest(request.RequestId);
            ShowStatus($"{request.SenderPlayerName}の申請を承認しました", false);
            RefreshRequestList();
        }

        /// <summary>
        /// 申請が拒否されたときの処理
        /// </summary>
        private void OnRequestRejected(FriendRequest request)
        {
            FriendManager.Instance.RejectFriendRequest(request.RequestId);
            ShowStatus($"{request.SenderPlayerName}の申請を拒否しました", false);
            RefreshRequestList();
        }

        /// <summary>
        /// 統計情報を更新する
        /// </summary>
        private void UpdateStatistics()
        {
            // 申請数
            if (requestCountText != null)
            {
                requestCountText.text = $"申請: {currentRequests.Count}件";
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