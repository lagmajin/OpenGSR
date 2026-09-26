using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Zenject;

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
        [Inject] private IInputService inputService;

        private void Awake()
        {
            if (inputService == null)
            {
                inputService = new UnityInputService();
            }
        }

        private void Start()
        {
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }
        }

        private void OnDisable()
        {
            ClearEntries();
            if (scoreboardPanel != null)
            {
                scoreboardPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (toggleKey == KeyCode.Tab ? inputService.IsScoreboardJustPressed() : Input.GetKeyDown(toggleKey))
            {
                ShowScoreboard();
            }
            else if (toggleKey == KeyCode.Tab ? inputService.IsScoreboardJustReleased() : Input.GetKeyUp(toggleKey))
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

            ClearEntries();

            // Fetch and sort players (by Kills desc, then Deaths asc)
            var players = PlayerRegistry.Instance.GetAllPlayers()
                .Where(p => p != null)
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

        private void ClearEntries()
        {
            foreach (var entry in activeEntries)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }

            activeEntries.Clear();
        }
    }
}
