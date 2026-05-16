

using OpenGSCore;
using System.Collections.Generic;
using UnityEngine;

namespace OpenGS
{

    //#ClientWaitRoom
    public class ClientWaitRoom
    {
        public string RoomName { get; set; }
        public string RoomId { get; set; }
        public int PlayerCount { get; set; } = 0;
        public int Capacity { get; set; } = 8;

        public List<OpenGSCore.PlayerInfo> PlayerList { get; set; } = new();

        public ClientWaitRoom()
        {

        }

        public ClientWaitRoom(string roomName,string roomId,int capacity)
        {
            RoomName = roomName;
            RoomId = roomId;

            Capacity = capacity;




        }

        public void AddNewPlayer(OpenGSCore.PlayerInfo info)
        {
            info.Name = RoomName;

            PlayerList.Add(info);



        }

        public void RemovePlayer(OpenGSCore.PlayerInfo info)
        {

        }

    }










}


