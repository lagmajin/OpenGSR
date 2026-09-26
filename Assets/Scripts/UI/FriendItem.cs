using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Globalization;

namespace OpenGS
{
    /// <summary>
    /// フレンドアイテムクラス
    /// フレンドリストの各アイテムを表示する
    /// </summary>
    public class FriendItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI lastOnlineText;
        [SerializeField] private Image statusIcon;
        [SerializeField] private Image playerAvatar;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button actionButton;

        // ─── 色設定 ─────────────────────────────────────────────────

        [Header("ステータス色設定")]
        [SerializeField] private Color onlineColor = new Color(0.2f, 0.8f, 0.2f); // 緑
        [SerializeField] private Color offlineColor = new Color(0.5f, 0.5f, 0.5f); // グレー
        [SerializeField] private Color awayColor = new Color(0.8f, 0.8f, 0.2f); // 黄

        // ─── 内部状態 ───────────────────────────────────────────────

        private FriendEntry friend;
        private Action<FriendEntry> onSelected;
        private Action<FriendEntry> onActionRequested;
        private bool isSelected = false;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// フレンドアイテムをセットアップする
        /// </summary>
        /// <param name="friend">フレンド情報</param>
        /// <param name="onSelectedCallback">選択時のコールバック</param>
        public void Setup(FriendEntry friend, Action<FriendEntry> onSelectedCallback)
        {
            Setup(friend, onSelectedCallback, null);
        }

        /// <summary>
        /// フレンド情報と選択・アクションのコールバックをセットアップする。
        /// </summary>
        public void Setup(
            FriendEntry friend,
            Action<FriendEntry> onSelectedCallback,
            Action<FriendEntry> onActionRequestedCallback)
        {
            if (friend == null)
            {
                this.friend = null;
                onSelected = null;
                onActionRequested = null;
                if (selectButton != null) selectButton.interactable = false;
                if (actionButton != null) actionButton.interactable = false;
                return;
            }

            this.friend = friend;
            this.onSelected = onSelectedCallback;
            this.onActionRequested = onActionRequestedCallback;
            isSelected = false;

            UpdateUI();
            SetupListeners();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            // プレイヤー名
            if (playerNameText != null)
            {
                playerNameText.text = friend.PlayerName;
            }

            // ステータス
            if (statusText != null)
            {
                statusText.text = friend.IsOnline ? "オンライン" : "オフライン";
            }

            // ステータスアイコンの色
            if (statusIcon != null)
            {
                statusIcon.color = friend.IsOnline ? onlineColor : offlineColor;
            }

            // 最終オンライン日時
            if (lastOnlineText != null)
            {
                if (friend.IsOnline)
                {
                    lastOnlineText.text = "オンライン中";
                }
                else
                {
                    lastOnlineText.text = FormatLastOnline(friend.LastOnlineDate);
                }
            }
        }

        /// <summary>
        /// リスナーを設定する
        /// </summary>
        private void SetupListeners()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnSelectButtonClicked);
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(OnActionButtonClicked);
                actionButton.onClick.AddListener(OnActionButtonClicked);
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnSelectButtonClicked()
        {
            isSelected = !isSelected;
            try
            {
                onSelected?.Invoke(friend);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendItem] Selection callback failed: {ex}");
            }
        }

        private void OnActionButtonClicked()
        {
            if (friend == null) return;

            if (onActionRequested != null)
            {
                try
                {
                    onActionRequested.Invoke(friend);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FriendItem] Action callback failed: {ex}");
                }
                return;
            }

            Debug.LogWarning($"[FriendItem] アクション処理が未接続です: {friend.PlayerName}");
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// 最終オンライン日時をフォーマットする
        /// </summary>
        private string FormatLastOnline(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return "不明";

            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind, out var lastOnline))
            {
                var now = DateTime.Now;
                var diff = now - lastOnline;

                if (diff.TotalMinutes < 1)
                    return "たった今";
                else if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes}分前";
                else if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours}時間前";
                else if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays}日前";
                else
                    return lastOnline.ToString("MM/dd HH:mm");
            }

            return dateString;
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// フレンド情報を取得する
        /// </summary>
        /// <returns>フレンド情報</returns>
        public FriendEntry GetFriend()
        {
            return friend;
        }

        /// <summary>
        /// 選択状態を設定する
        /// </summary>
        /// <param name="selected">選択状態</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
        }

        /// <summary>
        /// 選択状態を取得する
        /// </summary>
        /// <returns>選択状態</returns>
        public bool IsSelected()
        {
            return isSelected;
        }
    }
}
