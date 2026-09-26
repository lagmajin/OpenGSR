using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenGS
{
    /// <summary>
    /// リザルト画面で各プレイヤーの成績（1行分）を表示するUIコンポーネント。
    /// プレハブのルートオブジェクトにアタッチして使用する。
    /// </summary>
    public class MatchResultRowUI : MonoBehaviour
    {
        [Header("Text Elements")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI killsText;
        public TextMeshProUGUI deathsText;

        [Header("Background (Optional)")]
        [Tooltip("チームごとに色を変えたい背景パネル")]
        public Image backgroundPanel; 
        
        [Header("Team Colors")]
        public Color redTeamColor = new Color(1f, 0.4f, 0.4f, 0.5f);
        public Color blueTeamColor = new Color(0.4f, 0.4f, 1f, 0.5f);
        public Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        /// <summary>
        /// AbstractPlayer から UI にセットする
        /// </summary>
        public void SetData(AbstractPlayer player)
        {
            if (player == null) return;

            if (nameText != null) nameText.text = player.gameObject.name;
            int kills = player.Status?.KillCount ?? 0;
            int deaths = player.Status?.DeathCount ?? 0;
            if (scoreText != null) scoreText.text = kills.ToString();
            if (killsText != null) killsText.text = kills.ToString();
            if (deathsText != null) deathsText.text = deaths.ToString();

            if (backgroundPanel != null)
            {
                var team = player.Team();
                if (team == ETeam.Red) backgroundPanel.color = redTeamColor;
                else if (team == ETeam.Blue) backgroundPanel.color = blueTeamColor;
                else backgroundPanel.color = defaultColor;
            }
        }

        /// <summary>
        /// PlayerMatchResultData を UI にセットする
        /// </summary>
        public void SetData(PlayerMatchResultData data)
        {
            if (data == null)
            {
                ClearData();
                return;
            }

            if (nameText != null) nameText.text = data.PlayerName;
            if (scoreText != null) scoreText.text = data.Score.ToString();
            if (killsText != null) killsText.text = data.Kills.ToString();
            if (deathsText != null) deathsText.text = data.Deaths.ToString();

            // チーム別に背景色を変える
            if (backgroundPanel != null)
            {
                if (data.Team == "Red")
                {
                    backgroundPanel.color = redTeamColor;
                }
                else if (data.Team == "Blue")
                {
                    backgroundPanel.color = blueTeamColor;
                }
                else
                {
                    backgroundPanel.color = defaultColor;
                }
            }
        }

        private void ClearData()
        {
            if (nameText != null) nameText.text = string.Empty;
            if (scoreText != null) scoreText.text = "0";
            if (killsText != null) killsText.text = "0";
            if (deathsText != null) deathsText.text = "0";
            if (backgroundPanel != null) backgroundPanel.color = defaultColor;
        }
    }
}
