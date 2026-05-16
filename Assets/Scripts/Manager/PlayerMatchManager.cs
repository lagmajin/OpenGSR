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

        private List<PlayerAllData> players = new List<PlayerAllData>();
        public PlayerAllData(OpenGSCore.PlayerInfo playerInfo, PlayerStatus status)
        {
            this.playerInfo = playerInfo;
            this.status = status;
        }
    }



    //#プレイヤークラス
    public class PlayerMatchManager 
    {
        
      
        public PlayerMatchManager()
        {

        }

        public void AddPlayer(OpenGSCore.PlayerInfo info, PlayerStatus status)
        {
            //PlayerAllData newPlayer = new PlayerAllData(info, status);
            //players.Add(newPlayer);
        }

        public void RemovePlayer()
        {

        }

        public PlayerAllData MyPlayer()
        {

            return null;
        }

        public void RemoveAll()
        {
            //database.RemoveAll();
        }

        
    }
}
