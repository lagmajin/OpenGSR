

using Newtonsoft.Json.Linq;
using OpenGSCore;
using System.Collections.Generic;

namespace OpenGS
{

//#WaitManager
    public class WaitRoomManager
    {
        //public static WaitRoomManager Instance { get; } = new();

        public ClientWaitRoom WaitRoom { get; set; } = new();


        public ClientWaitRoom CreateNewWaitRoom(string name, string id, int capacity)
        {
            var waitRoom = new ClientWaitRoom(name, id, capacity);
            this.WaitRoom = waitRoom;
            return waitRoom;
        }

        public void CreateNewWaitRoomFromJson(JObject json)
        {
            IDictionary<string, JToken> dic = json;

            var roomName=dic["RoomInfo"]["RoomName"].ToString();

            var roomId = dic["RoomInfo"]["RoomId"].ToString();

            //var player = dic["RoomInfo"][""];
            var playerCount = dic["RoomInfo"]["WaitingPlayerCount"].ToString();

           var newRoom=new ClientWaitRoom();

            newRoom.RoomName= roomName;
            newRoom.RoomId = roomId;



        }
        
    

    }
}
