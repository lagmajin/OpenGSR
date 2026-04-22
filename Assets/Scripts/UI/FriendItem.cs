using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
        private bool isSelected = false;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// フレンドアイテムをセットアップする
        /// </summary>
        /// <param name="friend">フレンド情報</param>
        /// <param name="onSelectedCallback">選択時のコールバック</param>
        public void Setup(FriendEntry friend, Action<FriendEntry> onSelectedCallback)
        {
            this.friend = friend;
            this.onSelected = onSelectedCallback;

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
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            }

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnActionButtonClicked);
            }
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnSelectButtonClicked()
        {
            isSelected = !isSelected;
            onSelected?.Invoke(friend);
        }

        private void OnActionButtonClicked()
        {
            // アクションメニューを表示（実装は省略）
            Debug.Log($"[FriendItem] アクションボタンがクリックされました: {friend.PlayerName}");
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// 最終オンライン日時をフォーマットする
        /// </summary>
        private string FormatLastOnline(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return "不明";

            try
            {
                var lastOnline = DateTime.Parse(dateString);
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
            catch
            {
                return dateString;
            }
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