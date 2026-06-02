using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using OpenGSCore;
using System;
using UnityEngine;

namespace OpenGS
{
    public partial class MatchRoomManager
    {
        [CanBeNull] public WaitRoom OnlineWaitRoom { get; private set; } = null;
        [CanBeNull] public MatchRoom OnlineMatchRoom { get; private set; } = null;
        public WeaponLimit WeaponLimit { get; } = new();

        private readonly object _lockObj = new object();

        public void CreateNewOnlineWaitRoom(in string roomName = "", int capacity = 8)
        {
            lock (_lockObj)
            {
                if (OnlineWaitRoom != null)
                {
                    RemoveOnlineWaitRoom();
                }

                OnlineWaitRoom = new WaitRoom(roomName, "", capacity);
                WeaponLimit.Clear();
            }
        }

        public void RemoveOnlineWaitRoom()
        {
            lock (_lockObj)
            {
                OnlineWaitRoom = null;
            }
        }

        public void CreateNewOnlineMatchRoom(in string id)
        {
            lock (_lockObj)
            {
                if (OnlineMatchRoom != null)
                {
                    RemoveOnlineMatchRoom();
                }

                var roomId = string.IsNullOrWhiteSpace(id)
                    ? Guid.NewGuid().ToString("N")
                    : id;
                var roomName = OnlineWaitRoom != null && !string.IsNullOrWhiteSpace(OnlineWaitRoom.RoomName)
                    ? OnlineWaitRoom.RoomName
                    : "Online Match";
                var capacity = OnlineWaitRoom != null && OnlineWaitRoom.Capacity > 0
                    ? OnlineWaitRoom.Capacity
                    : 8;

                Debug.Log($"OnlineMatchRoom created... id={roomId}, name={roomName}, capacity={capacity}");
                OnlineMatchRoom = new MatchRoom(roomId)
                {
                    RoomName = roomName,
                    Capacity = capacity
                };
            }
        }

        public void CreateNewOnlineMatchRoom()
        {
            CreateNewOnlineMatchRoom(Guid.NewGuid().ToString("N"));
        }

        public void RemoveOnlineMatchRoom()
        {
            lock (_lockObj)
            {
                OnlineMatchRoom = null;
            }
        }

        public bool IsValidOnlineWaitRoom()
        {
            return OnlineWaitRoom != null;
        }

        public bool IsValidOnlineMatchRoom()
        {
            return OnlineMatchRoom != null;
        }
    }

    public partial class MatchRoomManager : IMatchRoomManager
    {
        [CanBeNull] public WaitRoom WaitRoom { get; private set; } = null;
        [CanBeNull] public MatchRoom OfflineMatchRoom { get; private set; } = null;
        [CanBeNull] public OfflineWaitRoom OfflineWaitRoom { get; private set; } = null;

        public MatchRoom TestRoom;
        public MapInfo MapInfo { get; set; }
        public JObject LastOfflineMatchResult { get; private set; }

        public MatchRoomManager()
        {
            SetUpDebugMatchRoom();
        }

        private void SetUpDebugMatchRoom()
        {
            TestRoom = new MatchRoom("test");
        }

        public void CreateNewOfflineWaitRoom(in string roomName = "")
        {
            lock (_lockObj)
            {
                if (OfflineWaitRoom == null)
                {
                    OfflineWaitRoom = new OfflineWaitRoom();
                }

                var select = GameModeSelectManager.Instance.OfflineGameSelect;
                var capacity = select != null && select.Capacity > 0 ? select.Capacity : 8;
                var resolvedRoomName = string.IsNullOrWhiteSpace(roomName) ? "OfflineRoom" : roomName;

                WaitRoom = new WaitRoom(resolvedRoomName, Guid.NewGuid().ToString(), capacity);
                WaitRoom.ChangeGameMode(select?.GameMode ?? EGameMode.DeathMatch);
                WeaponLimit.Clear();

                MapInfo = new MapInfo
                {
                    GameMode = select?.GameMode ?? EGameMode.DeathMatch,
                    Map = select?.Map ?? EMap.DryDays
                };
            }
        }

        public void CreateNewOfflineMatchRoom()
        {
            lock (_lockObj)
            {
                if (OfflineMatchRoom != null)
                {
                    RemoveOfflineMatchRoom();
                }

                if (WaitRoom == null)
                {
                    CreateNewOfflineWaitRoom("OfflineRoom");
                }

                OfflineMatchRoom = new MatchRoom(Guid.NewGuid().ToString())
                {
                    RoomName = "Offline Match",
                    Capacity = WaitRoom != null ? WaitRoom.Capacity : 8
                };

                LastOfflineMatchResult = null;
            }
        }

        public void RemoveOfflineWaitRoom()
        {
            lock (_lockObj)
            {
                WaitRoom = null;
                OfflineWaitRoom = null;
            }
        }

        public void RemoveOfflineMatchRoom()
        {
            lock (_lockObj)
            {
                OfflineMatchRoom = null;
            }
        }

        public bool IsValidOfflineWaitRoom()
        {
            return WaitRoom != null;
        }

        public bool IsValidOfflineMatchRoom()
        {
            return OfflineMatchRoom != null;
        }

        public void StoreOfflineMatchResult(JObject result)
        {
            LastOfflineMatchResult = result;
        }
    }
}
