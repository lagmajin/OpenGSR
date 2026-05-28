

using Newtonsoft.Json.Linq;
using OpenGSCore;
using System;
using System.Collections.Generic;

namespace OpenGS
{

//#WaitManager
    public class WaitRoomManager
    {
        //public static WaitRoomManager Instance { get; } = new();

        public ClientWaitRoom WaitRoom { get; set; } = new();


        public ClientWaitRoom CreateNewWaitRoom(string name, string id, int capacity, int playerCount = 1, EGameMode gameMode = EGameMode.DeathMatch, string ownerId = "", bool teamBalance = false, EMap map = EMap.Unknown, string password = "")
        {
            var waitRoom = new ClientWaitRoom(name, id, capacity);
            waitRoom.PlayerCount = playerCount > 0 ? playerCount : 1;
            waitRoom.GameMode = gameMode;
            waitRoom.OwnerId = ownerId ?? "";
            waitRoom.TeamBalance = teamBalance;
            waitRoom.Map = map;
            waitRoom.Password = password ?? "";
            this.WaitRoom = waitRoom;
            return waitRoom;
        }

        public void CreateNewWaitRoomFromJson(JObject json)
        {
            var roomInfo = RoomInfoSnapshot.FromJson(json);
            var newRoom = new ClientWaitRoom
            {
                RoomName = roomInfo.RoomName,
                RoomId = roomInfo.RoomId,
                Capacity = roomInfo.Capacity,
                PlayerCount = roomInfo.PlayerCount > 0 ? roomInfo.PlayerCount : 1,
                OwnerId = roomInfo.OwnerId,
                GameMode = Enum.TryParse(roomInfo.GameMode, true, out EGameMode gameMode) ? gameMode : EGameMode.DeathMatch,
                Map = Enum.TryParse(roomInfo.Map, true, out EMap map) ? map : EMap.Unknown,
                TeamBalance = roomInfo.TeamBalance,
                Password = json["Password"]?.ToString() ?? ""
            };

            this.WaitRoom = newRoom;
        }
        
    

    }
}
