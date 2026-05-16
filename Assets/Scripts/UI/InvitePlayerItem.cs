using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OpenGS
{
    /// <summary>
    /// 招待プレイヤーアイテムクラス
    /// プレイヤーリストの各アイテムを表示する
    /// </summary>
    public class InvitePlayerItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image statusImage;
        [SerializeField] private Image selectedBackground;
        [SerializeField] private Button selectButton;

        // ─── 内部状態 ───────────────────────────────────────────────

        private OpenGSCore.PlayerInfo playerInfo;
        private Action<OpenGSCore.PlayerInfo> onSelected;
        private bool isSelected = false;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// プレイヤーアイテムをセットアップする
        /// </summary>
        /// <param name="player">プレイヤー情報</param>
        /// <param name="onSelectedCallback">選択時のコールバック</param>
        public void Setup(OpenGSCore.PlayerInfo player, Action<OpenGSCore.PlayerInfo> onSelectedCallback)
        {
            playerInfo = player;
            onSelected = onSelectedCallback;

            UpdateUI();
            SetupListeners();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            if (playerInfo == null) return;

            // プレイヤー名
            if (playerNameText != null)
            {
                playerNameText.text = playerInfo.Name;
            }

            // レベル
            if (levelText != null)
            {
                levelText.text = $"Lv.{playerInfo.Level}";
            }

            // ステータス画像
            if (statusImage != null)
            {
                if (playerInfo.IsReady)
                {
                    statusImage.color = Color.yellow; // ルーム参加中
                }
                else if (!string.IsNullOrEmpty(playerInfo.CurrentIp))
                {
                    statusImage.color = Color.green; // オンライン
                }
                else
                {
                    statusImage.color = Color.gray; // オフライン
                }
            }

            // 選択背景を非表示
            if (selectedBackground != null)
            {
                selectedBackground.gameObject.SetActive(false);
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
        }

        // ─── イベントハンドラ ─────────────────────────────────────────

        private void OnSelectButtonClicked()
        {
            if (playerInfo == null) return;

            // 選択状態を切り替え
            isSelected = !isSelected;

            if (selectedBackground != null)
            {
                selectedBackground.gameObject.SetActive(isSelected);
            }

            // コールバックを発火
            if (isSelected)
            {
                onSelected?.Invoke(playerInfo);
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// 選択状態を設定する
        /// </summary>
        /// <param name="selected">選択状態</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectedBackground != null)
            {
                selectedBackground.gameObject.SetActive(isSelected);
            }
        }

        /// <summary>
        /// プレイヤー情報を取得する
        /// </summary>
        /// <returns>プレイヤー情報</returns>
        public OpenGSCore.PlayerInfo GetPlayerInfo()
        {
            return playerInfo;
        }
    }
}
