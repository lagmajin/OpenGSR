using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Globalization;

namespace OpenGS
{
    /// <summary>
    /// フレンド申請アイテムクラス
    /// フレンド申請リストの各アイテムを表示する
    /// </summary>
    public class FriendRequestItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI senderNameText;
        [SerializeField] private TextMeshProUGUI requestDateText;
        [SerializeField] private Image senderAvatar;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        // ─── 色設定 ─────────────────────────────────────────────────

        [Header("ボタン色設定")]
        [SerializeField] private Color acceptColor = new Color(0.2f, 0.8f, 0.2f); // 緑
        [SerializeField] private Color rejectColor = new Color(0.8f, 0.2f, 0.2f); // 赤

        // ─── 内部状態 ───────────────────────────────────────────────

        private FriendRequest request;
        private Action<FriendRequest> onAccepted;
        private Action<FriendRequest> onRejected;
        private bool actionInProgress;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// フレンド申請アイテムをセットアップする
        /// </summary>
        /// <param name="request">フレンド申請情報</param>
        /// <param name="onAcceptedCallback">承認時のコールバック</param>
        /// <param name="onRejectedCallback">拒否時のコールバック</param>
        public void Setup(FriendRequest request, Action<FriendRequest> onAcceptedCallback, Action<FriendRequest> onRejectedCallback)
        {
            if (request == null)
            {
                this.request = null;
                onAccepted = null;
                onRejected = null;
                actionInProgress = false;
                if (acceptButton != null) acceptButton.interactable = false;
                if (rejectButton != null) rejectButton.interactable = false;
                return;
            }

            this.request = request;
            this.onAccepted = onAcceptedCallback;
            this.onRejected = onRejectedCallback;
            actionInProgress = false;

            UpdateUI();
            SetupListeners();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            // 送信者名
            if (senderNameText != null)
            {
                senderNameText.text = request.SenderPlayerName;
            }

            // 申請日時
            if (requestDateText != null)
            {
                requestDateText.text = FormatRequestDate(request.RequestDate);
            }

            // 承認ボタンの色
            if (acceptButton != null)
            {
                var colors = acceptButton.colors;
                colors.normalColor = acceptColor;
                colors.highlightedColor = acceptColor * 1.2f;
                acceptButton.colors = colors;
            }

            // 拒否ボタンの色
            if (rejectButton != null)
            {
                var colors = rejectButton.colors;
                colors.normalColor = rejectColor;
                colors.highlightedColor = rejectColor * 1.2f;
                rejectButton.colors = colors;
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveListener(OnAcceptButtonClicked);
                acceptButton.onClick.AddListener(OnAcceptButtonClicked);
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.RemoveListener(OnRejectButtonClicked);
                rejectButton.onClick.AddListener(OnRejectButtonClicked);
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnAcceptButtonClicked()
        {
            if (actionInProgress || request == null)
            {
                return;
            }

            actionInProgress = true;
            if (acceptButton != null) acceptButton.interactable = false;
            if (rejectButton != null) rejectButton.interactable = false;
            try
            {
                onAccepted?.Invoke(request);
            }
            catch (Exception ex)
            {
                actionInProgress = false;
                if (acceptButton != null) acceptButton.interactable = true;
                if (rejectButton != null) rejectButton.interactable = true;
                Debug.LogError($"[FriendRequestItem] Accept callback failed: {ex}");
            }
        }

        private void OnRejectButtonClicked()
        {
            if (actionInProgress || request == null)
            {
                return;
            }

            actionInProgress = true;
            if (acceptButton != null) acceptButton.interactable = false;
            if (rejectButton != null) rejectButton.interactable = false;
            try
            {
                onRejected?.Invoke(request);
            }
            catch (Exception ex)
            {
                actionInProgress = false;
                if (acceptButton != null) acceptButton.interactable = true;
                if (rejectButton != null) rejectButton.interactable = true;
                Debug.LogError($"[FriendRequestItem] Reject callback failed: {ex}");
            }
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// 申請日時をフォーマットする
        /// </summary>
        private string FormatRequestDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return "不明";

            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var requestDate))
            {
                var now = DateTime.Now;
                var diff = now - requestDate;

                if (diff.TotalMinutes < 1)
                    return "たった今";
                else if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes}分前";
                else if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours}時間前";
                else if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays}日前";
                else
                    return requestDate.ToString("MM/dd HH:mm");
            }

            return dateString;
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// フレンド申請情報を取得する
        /// </summary>
        /// <returns>フレンド申請情報</returns>
        public FriendRequest GetRequest()
        {
            return request;
        }
    }
}
