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

        public void RemovePlayer()
        {
            if (players.Count > 0)
            {
                players.RemoveAt(players.Count - 1);
                return;
            }

            Debug.LogWarning("[PlayerMatchManager] RemovePlayer called but no players are registered.");
        }

        public PlayerAllData MyPlayer()
        {
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
        }

        
    }
}
