using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{
    public interface ITeamRespawnPoints : IRespawnPoints
    {
        Vector2 RandomBlueTeam();
        Vector2 RandomRedTeam();
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MultipleTags))]
    public class TeamReSpawnPoints : MonoBehaviour, ITeamRespawnPoints
    {
        [SerializeField, Min(0)] private int recentSpawnHistorySize = 3;
        [SerializeField, Min(0f)] private float recentSpawnPenalty = 8f;
        [SerializeField, Min(0f)] private float enemyProximityWeight = 1.25f;
        [SerializeField, Min(0f)] private float allySpacingWeight = 1f;

        public List<GameObject> BlueTeamPoints;
        public List<GameObject> RedTeamPoints;

        private readonly Queue<Vector2> blueRecentSpawns = new Queue<Vector2>();
        private readonly Queue<Vector2> redRecentSpawns = new Queue<Vector2>();

        private void Start()
        {
        }

        public Vector2 GetRandomSpawnPoint(ETeam team = ETeam.NoTeam)
        {
            return team == ETeam.Red ? RandomRedTeam() : RandomBlueTeam();
        }

        public int Count(ETeam team = ETeam.NoTeam)
        {
            if (team == ETeam.Red)
            {
                return RedTeamPoints != null ? RedTeamPoints.Count : 0;
            }

            if (team == ETeam.Blue)
            {
                return BlueTeamPoints != null ? BlueTeamPoints.Count : 0;
            }

            return Count();
        }

        public Vector2 RandomBlueTeam()
        {
            return RandomFromList(BlueTeamPoints, ETeam.Blue);
        }

        public Vector2 RandomRedTeam()
        {
            return RandomFromList(RedTeamPoints, ETeam.Red);
        }

        public int Count()
        {
            return (BlueTeamPoints != null ? BlueTeamPoints.Count : 0)
                 + (RedTeamPoints != null ? RedTeamPoints.Count : 0);
        }

        public List<string> ReadTeamRespawn()
        {
            List<string> result = new List<string>();
            return result;
        }

        private Vector2 RandomFromList(List<GameObject> points, ETeam team)
        {
            if (points == null || points.Count == 0)
            {
                return Vector2.zero;
            }

            var validPoints = new List<GameObject>(points.Count);
            foreach (var point in points)
            {
                if (point != null)
                {
                    validPoints.Add(point);
                }
            }

            if (validPoints.Count == 0)
            {
                return Vector2.zero;
            }

            var teamPlayers = PlayerRegistry.Instance != null
                ? PlayerRegistry.Instance.GetPlayersByTeam(team)
                : null;
            var enemyPlayers = PlayerRegistry.Instance != null
                ? PlayerRegistry.Instance.GetPlayersByTeam(team == ETeam.Red ? ETeam.Blue : ETeam.Red)
                : null;

            GameObject bestPoint = null;
            float bestScore = float.NegativeInfinity;

            foreach (var point in validPoints)
            {
                var score = ScoreSpawnPoint(point.transform.position, teamPlayers, enemyPlayers, team);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = point;
                }
            }

            if (bestPoint != null)
            {
                RecordSpawn(team, bestPoint.transform.position);
                return bestPoint.transform.position;
            }

            var rand = Random.Range(0, validPoints.Count);
            var fallback = validPoints[rand].transform.position;
            RecordSpawn(team, fallback);
            return fallback;
        }

        private float ScoreSpawnPoint(
            Vector2 spawnPoint,
            IReadOnlyCollection<AbstractPlayer> teamPlayers,
            IReadOnlyCollection<AbstractPlayer> enemyPlayers,
            ETeam team)
        {
            var score = Random.value;

            var allyDistance = EvaluateDistance(spawnPoint, teamPlayers);
            if (allyDistance.HasValue)
            {
                score += allyDistance.Value * allySpacingWeight;
            }

            var enemyDistance = EvaluateDistance(spawnPoint, enemyPlayers);
            if (enemyDistance.HasValue)
            {
                score += enemyDistance.Value * enemyProximityWeight;
            }

            score -= EvaluateRecentSpawnPenalty(spawnPoint, team);

            return score;
        }

        private float EvaluateRecentSpawnPenalty(Vector2 spawnPoint, ETeam team)
        {
            var history = GetRecentSpawnHistory(team);
            if (history == null || history.Count == 0)
            {
                return 0f;
            }

            var minDistance = float.PositiveInfinity;
            foreach (var recent in history)
            {
                var distance = Vector2.Distance(spawnPoint, recent);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            if (float.IsInfinity(minDistance))
            {
                return 0f;
            }

            return Mathf.Max(0f, recentSpawnPenalty - minDistance);
        }

        private static float? EvaluateDistance(Vector2 spawnPoint, IReadOnlyCollection<AbstractPlayer> players)
        {
            if (players == null || players.Count == 0)
            {
                return null;
            }

            var minDistance = float.PositiveInfinity;
            var hasLivingPlayer = false;

            foreach (var player in players)
            {
                if (player == null || player.IsDead())
                {
                    continue;
                }

                hasLivingPlayer = true;
                var distance = Vector2.Distance(spawnPoint, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return hasLivingPlayer ? minDistance : null;
        }

        private Queue<Vector2> GetRecentSpawnHistory(ETeam team)
        {
            return team == ETeam.Red ? redRecentSpawns : blueRecentSpawns;
        }

        private void RecordSpawn(ETeam team, Vector2 spawnPoint)
        {
            var history = GetRecentSpawnHistory(team);
            history.Enqueue(spawnPoint);

            while (history.Count > recentSpawnHistorySize)
            {
                history.Dequeue();
            }
        }
    }
}
