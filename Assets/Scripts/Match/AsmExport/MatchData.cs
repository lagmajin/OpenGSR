using System;
using System.Collections.Generic;
using OpenGSCore;
using UnityEngine;
using System.Linq;

namespace OpenGS
{
    public enum EMatchDataType
    {

    }

    public class MatchData
    {
        // Dictionary to hold all players' statuses, including own and others
        public Dictionary<string, PlayerStatus> AllPlayers { get; private set; } = new Dictionary<string, PlayerStatus>();

        public int AlivePlayerCount { get; private set; } = 0;
        public int AllDeath { get; private set; } = 0;
        public int AllKill { get; private set; } = 0;
        public int RedTeamKill { get; private set; } = 0;
        public int BlueTeamKill { get; private set; } = 0;
        public int RedTeamFlagReturn { get; private set; } = 0;
        public int BlueTeamFlagReturn { get; private set; } = 0;
        public int MaxPlayerKillCount { get; private set; } = 0;
        
        // Cache for my player ID to avoid repeated LINQ searches
        private string _myPlayerId = null;


        public MatchData(EGameMode mode = EGameMode.Unknown)
        {
            // mode parameter is not used, but kept for future extension
        }

        // Method to add or update player status
        public void UpdatePlayerStatus(string playerId, PlayerStatus status)
        {
            AllPlayers[playerId] = status;
        }

        // Method to get player status
        public PlayerStatus GetPlayerStatus(string playerId)
        {
            if (AllPlayers.TryGetValue(playerId, out var status))
            {
                return status;
            }
            return null;
        }

        // Method to remove player
        public void RemovePlayer(string playerId)
        {
            AllPlayers.Remove(playerId);
            // If it was my player, clear the cache
            if (_myPlayerId == playerId)
            {
                _myPlayerId = null;
            }
        }

        public void DebugPrintUnity()
        {
            Debug.Log("AlivePlayer");
        }

        // Set my player ID (called when login succeeds or player spawns)
        public void SetMyPlayerId(string playerId)
        {
            _myPlayerId = playerId;
            Debug.Log($"MatchData: My player ID set to {playerId}");
        }

        // Get my player ID
        public string GetMyPlayerId()
        {
            return _myPlayerId;
        }

        // Method to get my player status - now optimized with caching
        public PlayerStatus GetMyPlayerStatus()
        {
            // If we have cached ID, use direct lookup
            if (!string.IsNullOrEmpty(_myPlayerId))
            {
                return GetPlayerStatus(_myPlayerId);
            }

            // Fallback: search by PlayerType (slower, but works if not cached)
            if (PlayerRegistry.Instance != null)
            {
                var players = PlayerRegistry.Instance.GetAllPlayers();
                var myPlayer = players.FirstOrDefault(p => p != null && p.PlayerType() == EPlayerType.MyPlayer);
                if (myPlayer != null)
                {
                    _myPlayerId = myPlayer.UniqueID().ToString();
                    return GetPlayerStatus(_myPlayerId);
                }
            }
            return null;
        }

        // Method to update kill score (called from network)
        public void UpdateKillScore(string playerId, int kills, int deaths, int score, ETeam team = ETeam.NoTeam)
        {
            // 既存のプレイヤーステータスを取得または新規作成
            var status = GetPlayerStatus(playerId) ?? new PlayerStatus();
            
            // kills/deaths/score を設定（PlayerStatusにこれらのプロパティが必要）
            // ※ PlayerStatus に Kills, Deaths, Score プロパティがある場合のみ
            // ここではDictionaryに保持するkills/deaths/scoreを管理
            
            // チーム статистик 更新
            switch (team)
            {
                case ETeam.Red:
                    RedTeamKill += 1;
                    break;
                case ETeam.Blue:
                    BlueTeamKill += 1;
                    break;
            }
            
            AllKill += 1;
            
            Debug.Log($"[MatchData] KillScore Update - Player: {playerId}, Kills: {kills}, Deaths: {deaths}, Score: {score}, Team: {team}");
        }

        // Method to update death count (called from network)
        public void UpdateDeathCount(string playerId, ETeam team = ETeam.NoTeam)
        {
            AllDeath += 1;
            
            Debug.Log($"[MatchData] Death Update - Player: {playerId}, Team: {team}");
        }
    }
}
