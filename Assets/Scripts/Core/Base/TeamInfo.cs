
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    public class TeamInfo : MonoBehaviour
    {
        [SerializeField] private ETeam team = ETeam.NoTeam;
        [SerializeField] private Color teamColor = Color.white;
        [SerializeField] private int initialScore = 0;

        private int score;
        private readonly List<GameObject> players = new List<GameObject>();

        public event Action<int> OnScoreChanged;
        public event Action<int> OnPlayerCountChanged;

        public ETeam Team => team;
        public Color TeamColor => teamColor;

        public int Score => score;

        public IReadOnlyList<GameObject> Players => players.AsReadOnly();

        void Awake()
        {
            score = initialScore;
        }

        public void SetTeam(ETeam t)
        {
            team = t;
        }

        public void SetColor(Color c)
        {
            teamColor = c;
        }

        public void AddScore(int delta)
        {
            if (delta == 0) return;
            score += delta;
            OnScoreChanged?.Invoke(score);
        }

        public void ResetScore()
        {
            score = 0;
            OnScoreChanged?.Invoke(score);
        }

        public bool AddPlayer(GameObject player)
        {
            if (player == null) return false;
            if (players.Contains(player)) return false;
            players.Add(player);
            OnPlayerCountChanged?.Invoke(players.Count);
            return true;
        }

        public bool RemovePlayer(GameObject player)
        {
            if (player == null) return false;
            var removed = players.Remove(player);
            if (removed)
            {
                OnPlayerCountChanged?.Invoke(players.Count);
            }
            return removed;
        }

        public int PlayerCount() => players.Count;

        // Safe lookup by instance id
        public bool ContainsPlayer(GameObject player) => player != null && players.Contains(player);
    }
}
