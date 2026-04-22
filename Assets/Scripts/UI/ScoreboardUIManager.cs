using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

namespace OpenGS
{
    /// <summary>
    /// マッチ内の全プレイヤーのスコア（キル・デス）を表示するスコアボードUIマネージャー。
    /// Tabキー押下で表示・非表示を切り替える。
    /// </summary>
    public class ScoreboardUIManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private readonly List<GameObject> activeEntries = new List<GameObject>();

        private void Start()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ShowScoreboard();
            }
            else if (Input.GetKeyUp(toggleKey))
            {
                HideScoreboard();
            }
        }

        private void ShowScoreboard()
        {
            if (scoreboardPanel == null) return;

            scoreboardPanel.SetActive(true);
            RefreshScoreboard();
        }

        private void HideScoreboard()
        {
            if (scoreboardPanel == null) return;
            scoreboardPanel.SetActive(false);
        }

        private void RefreshScoreboard()
        {
            if (PlayerRegistry.Instance == null || entryContainer == null || entryPrefab == null) return;

            // Clear old entries
            foreach (var entry in activeEntries)
            {
                Destroy(entry);
            }
            activeEntries.Clear();

            // Fetch and sort players (by Kills desc, then Deaths asc)
            var players = PlayerRegistry.Instance.GetAllPlayers()
                .OrderByDescending(p => p.Status?.KillCount ?? 0)
                .ThenBy(p => p.Status?.DeathCount ?? 0)
                .ToList();

            foreach (var player in players)
            {
                var entryObj = Instantiate(entryPrefab, entryContainer);
                activeEntries.Add(entryObj);

                // Populate entry data
                var rowUI = entryObj.GetComponent<MatchResultRowUI>();
                if (rowUI != null)
                {
                    rowUI.SetData(player);
                }
            }
        }
    }
}
