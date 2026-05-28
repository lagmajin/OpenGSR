using System.Collections;
using UnityEngine;

using OpenGSCore;
using System.Collections.Generic;

namespace OpenGS
{
public class PlayerAllData
    {
        OpenGSCore.PlayerInfo playerInfo;
        PlayerStatus status;

        public PlayerAllData(OpenGSCore.PlayerInfo playerInfo, PlayerStatus status)
        {
            this.playerInfo = playerInfo;
            this.status = status;
        }

        public OpenGSCore.PlayerInfo PlayerInfo => playerInfo;
        public PlayerStatus Status => status;
    }



    //#プレイヤークラス
    public class PlayerMatchManager 
    {
        private readonly List<PlayerAllData> players = new List<PlayerAllData>();
        private string myPlayerId = string.Empty;
        
      
        public PlayerMatchManager()
        {

        }

        public void AddPlayer(OpenGSCore.PlayerInfo info, PlayerStatus status)
        {
            if (info == null || status == null)
            {
                Debug.LogWarning("[PlayerMatchManager] AddPlayer received null info or status.");
                return;
            }

            players.Add(new PlayerAllData(info, status));
        }

        public void SetMyPlayerId(string playerId)
        {
            myPlayerId = playerId ?? string.Empty;
        }

        public void RemovePlayer()
        {
            if (players.Count > 0)
            {
                var removed = players[players.Count - 1];
                players.RemoveAt(players.Count - 1);
                if (removed?.PlayerInfo != null && string.Equals(removed.PlayerInfo.Id, myPlayerId, System.StringComparison.OrdinalIgnoreCase))
                {
                    myPlayerId = string.Empty;
                }
                return;
            }

            Debug.LogWarning("[PlayerMatchManager] RemovePlayer called but no players are registered.");
        }

        public PlayerAllData MyPlayer()
        {
            if (!string.IsNullOrWhiteSpace(myPlayerId))
            {
                for (var index = 0; index < players.Count; index++)
                {
                    var player = players[index];
                    if (player?.PlayerInfo != null && string.Equals(player.PlayerInfo.Id, myPlayerId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return player;
                    }
                }
            }

            if (players.Count == 0)
            {
                Debug.LogWarning("[PlayerMatchManager] MyPlayer requested but no players are registered.");
                return null;
            }

            return players[players.Count - 1];
        }

        public void RemoveAll()
        {
            players.Clear();
            myPlayerId = string.Empty;
        }

        
    }
}
