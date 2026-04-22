using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OpenGS
{
    /// <summary>
    /// ランキングアイテムクラス
    /// ランキングリストの各アイテムを表示する
    /// </summary>
    public class RankingItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI gameModeText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image rankBackground;
        [SerializeField] private Image highlightBackground;

        // ─── 色設定 ─────────────────────────────────────────────────

        [Header("色設定")]
        [SerializeField] private Color firstPlaceColor = new Color(1f, 0.84f, 0f); // 金色
        [SerializeField] private Color secondPlaceColor = new Color(0.75f, 0.75f, 0.75f); // 銀色
        [SerializeField] private Color thirdPlaceColor = new Color(0.8f, 0.5f, 0.2f); // 銅色
        [SerializeField] private Color defaultColor = new Color(0.9f, 0.9f, 0.9f); // デフォルト
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.6f, 1f, 0.3f); // ハイライト

        // ─── 内部状態 ───────────────────────────────────────────────

        private RankingEntry entry;
        private bool isPlayerEntry = false;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// ランキングアイテムをセットアップする
        /// </summary>
        /// <param name="rank">順位</param>
        /// <param name="entry">ランキングエントリー</param>
        /// <param name="currentPlayerName">現在のプレイヤー名</param>
        public void Setup(int rank, RankingEntry entry, string currentPlayerName = null)
        {
            this.entry = entry;
            this.isPlayerEntry = !string.IsNullOrEmpty(currentPlayerName) && entry.PlayerName == currentPlayerName;

            UpdateUI(rank);
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI(int rank)
        {
            // 順位
            if (rankText != null)
            {
                rankText.text = $"{rank}位";
            }

            // 順位に応じた背景色
            if (rankBackground != null)
            {
                switch (rank)
                {
                    case 1:
                        rankBackground.color = firstPlaceColor;
                        break;
                    case 2:
                        rankBackground.color = secondPlaceColor;
                        break;
                    case 3:
                        rankBackground.color = thirdPlaceColor;
                        break;
                    default:
                        rankBackground.color = defaultColor;
                        break;
                }
            }

            // プレイヤー名
            if (playerNameText != null)
            {
                playerNameText.text = entry.PlayerName;
            }

            // スコア
            if (scoreText != null)
            {
                scoreText.text = entry.Score.ToString("N0");
            }

            // ゲームモード
            if (gameModeText != null)
            {
                gameModeText.text = GetGameModeDisplayName(entry.GameMode);
            }

            // タイムスタンプ
            if (timestampText != null)
            {
                timestampText.text = entry.Timestamp;
            }

            // ハイライト（現在のプレイヤー）
            if (highlightBackground != null)
            {
                highlightBackground.gameObject.SetActive(isPlayerEntry);
                if (isPlayerEntry)
                {
                    highlightBackground.color = highlightColor;
                }
            }
        }

        // ─── ユーティリティ ─────────────────────────────────────────

        /// <summary>
        /// ゲームモードの表示名を取得する
        /// </summary>
        private string GetGameModeDisplayName(string gameMode)
        {
            switch (gameMode)
            {
                case "DeathMatch":
                    return "DM";
                case "TeamDeathMatch":
                    return "TDM";
                case "CaptureTheFlag":
                    return "CTF";
                case "Survival":
                    return "SUV";
                case "TeamSurvival":
                    return "TSUV";
                case "Total":
                    return "合計";
                default:
                    return gameMode;
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// ランキングエントリーを取得する
        /// </summary>
        /// <returns>ランキングエントリー</returns>
        public RankingEntry GetEntry()
        {
            return entry;
        }

        /// <summary>
        /// 現在のプレイヤーのエントリーかどうか
        /// </summary>
        /// <returns>判定結果</returns>
        public bool IsPlayerEntry()
        {
            return isPlayerEntry;
        }
    }
}