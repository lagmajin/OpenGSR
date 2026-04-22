using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OpenGS
{
    /// <summary>
    /// マッチ履歴アイテムクラス
    /// マッチ履歴リストの各アイテムを表示する
    /// </summary>
    public class MatchHistoryItem : MonoBehaviour
    {
        // ─── UI要素 ─────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI gameModeText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI killsDeathsText;
        [SerializeField] private TextMeshProUGUI mapNameText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image resultBackground;
        [SerializeField] private Image gameModeIcon;

        // ─── 色設定 ─────────────────────────────────────────────────

        [Header("結果色設定")]
        [SerializeField] private Color winColor = new Color(0.2f, 0.8f, 0.2f); // 緑
        [SerializeField] private Color loseColor = new Color(0.8f, 0.2f, 0.2f); // 赤
        [SerializeField] private Color drawColor = new Color(0.8f, 0.8f, 0.2f); // 黄

        [Header("ゲームモード色設定")]
        [SerializeField] private Color dmColor = new Color(0.8f, 0.2f, 0.2f); // 赤
        [SerializeField] private Color tdmColor = new Color(0.2f, 0.2f, 0.8f); // 青
        [SerializeField] private Color ctfColor = new Color(0.8f, 0.8f, 0.2f); // 黄
        [SerializeField] private Color suvColor = new Color(0.2f, 0.8f, 0.2f); // 緑

        // ─── 内部状態 ───────────────────────────────────────────────

        private MatchHistoryEntry entry;

        // ─── 初期化 ─────────────────────────────────────────────────

        /// <summary>
        /// マッチ履歴アイテムをセットアップする
        /// </summary>
        /// <param name="entry">マッチ履歴エントリー</param>
        public void Setup(MatchHistoryEntry entry)
        {
            this.entry = entry;
            UpdateUI();
        }

        /// <summary>
        /// UIを更新する
        /// </summary>
        private void UpdateUI()
        {
            // ゲームモード
            if (gameModeText != null)
            {
                gameModeText.text = GetGameModeDisplayName(entry.GameMode);
            }

            // ゲームモードアイコンの色
            if (gameModeIcon != null)
            {
                gameModeIcon.color = GetGameModeColor(entry.GameMode);
            }

            // 結果
            if (resultText != null)
            {
                resultText.text = GetResultDisplayName(entry.Result);
            }

            // 結果背景の色
            if (resultBackground != null)
            {
                resultBackground.color = GetResultColor(entry.Result);
            }

            // スコア
            if (scoreText != null)
            {
                scoreText.text = $"スコア: {entry.Score:N0}";
            }

            // キル/デス
            if (killsDeathsText != null)
            {
                killsDeathsText.text = $"K/D: {entry.Kills}/{entry.Deaths}";
            }

            // マップ名
            if (mapNameText != null)
            {
                mapNameText.text = !string.IsNullOrEmpty(entry.MapName) ? entry.MapName : "-";
            }

            // タイムスタンプ
            if (timestampText != null)
            {
                timestampText.text = FormatTimestamp(entry.Timestamp);
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
                    return "デスマッチ";
                case "TeamDeathMatch":
                    return "チームデスマッチ";
                case "CaptureTheFlag":
                    return "キャプチャー・ザ・フラッグ";
                case "Survival":
                    return "サバイバル";
                case "TeamSurvival":
                    return "チームサバイバル";
                default:
                    return gameMode;
            }
        }

        /// <summary>
        /// ゲームモードの色を取得する
        /// </summary>
        private Color GetGameModeColor(string gameMode)
        {
            switch (gameMode)
            {
                case "DeathMatch":
                    return dmColor;
                case "TeamDeathMatch":
                    return tdmColor;
                case "CaptureTheFlag":
                    return ctfColor;
                case "Survival":
                    return suvColor;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// 結果の表示名を取得する
        /// </summary>
        private string GetResultDisplayName(string result)
        {
            switch (result)
            {
                case "Win":
                    return "勝利";
                case "Lose":
                    return "敗北";
                case "Draw":
                    return "引き分け";
                default:
                    return result;
            }
        }

        /// <summary>
        /// 結果の色を取得する
        /// </summary>
        private Color GetResultColor(string result)
        {
            switch (result)
            {
                case "Win":
                    return winColor;
                case "Lose":
                    return loseColor;
                case "Draw":
                    return drawColor;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// タイムスタンプをフォーマットする
        /// </summary>
        private string FormatTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
                return "-";

            try
            {
                var dateTime = DateTime.Parse(timestamp);
                return dateTime.ToString("MM/dd HH:mm");
            }
            catch
            {
                return timestamp;
            }
        }

        // ─── 公開メソッド ───────────────────────────────────────────

        /// <summary>
        /// マッチ履歴エントリーを取得する
        /// </summary>
        /// <returns>マッチ履歴エントリー</returns>
        public MatchHistoryEntry GetEntry()
        {
            return entry;
        }
    }
}