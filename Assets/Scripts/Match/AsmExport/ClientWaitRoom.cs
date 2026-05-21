

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
        public string OwnerId { get; set; } = "";
        public EGameMode GameMode { get; set; } = EGameMode.DeathMatch;
        public EMap Map { get; set; } = EMap.Unknown;
        public bool TeamBalance { get; set; } = false;

        public List<OpenGSCore.PlayerInfo> PlayerList { get; set; } = new();

        public ClientWaitRoom()
        {

        }

        public ClientWaitRoom(string roomName,string roomId,int capacity)
        {
            RoomName = roomName;
            RoomId = roomId;

            Capacity = capacity;
            PlayerCount = 1;
        }

        public void AddNewPlayer(OpenGSCore.PlayerInfo info)
        {
            if (info == null)
            {
                return;
            }

            var existingIndex = PlayerList.FindIndex(player => player != null && player.Id == info.Id);
            if (existingIndex >= 0)
            {
                PlayerList[existingIndex] = info;
            }
            else
            {
                PlayerList.Add(info);
            }

            PlayerCount = PlayerList.Count;
        }

        public void RemovePlayer(OpenGSCore.PlayerInfo info)
        {
            if (info == null)
            {
                return;
            }

            RemovePlayer(info.Id);
        }

        public void RemovePlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            PlayerList.RemoveAll(player => player != null && player.Id == playerId);
            PlayerCount = PlayerList.Count;
        }

    }










}


