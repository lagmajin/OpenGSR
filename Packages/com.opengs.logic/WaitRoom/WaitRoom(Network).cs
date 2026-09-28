#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public partial class WaitRoom
    {
        public bool HasSpace()
        {
            lock (lockObject)
            {
                return Capacity <= 0 || Players.Count < Capacity;
            }
        }

        public bool TryGetPlayer(string id, out PlayerInfo? player)
        {
            lock (lockObject)
            {
                return Players.TryGetValue(id, out player);
            }
        }

        public void SetRoomName(string roomName)
        {
            lock (lockObject)
            {
                RoomName = roomName ?? string.Empty;
            }
        }

        public void SetRoomCapacity(int capacity)
        {
            lock (lockObject)
            {
                Capacity = Math.Max(1, capacity);
            }
        }

        public WaitRoomSnapshot ToSnapshot()
        {
            lock (lockObject)
            {
                var snapshot = new WaitRoomSnapshot
                {
                    RoomId = RoomId,
                    RoomName = RoomName,
                    Capacity = Capacity,
                    NowPlaying = NowPlaying,
                    GameMode = GameMode.ToString(),
                    Map = Map.ToString(),
                    TeamBalance = setting is AbstractTeamMatchSetting teamSetting && teamSetting.TeamBalance,
                    OwnerId = ""
                };

                foreach (var player in Players.Values)
                {
                    if (player != null)
                    {
                        snapshot.Players.Add(player);
                    }
                }

                return snapshot;
            }
        }

        public void ApplySnapshot(WaitRoomSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            lock (lockObject)
            {
                RoomId = snapshot.RoomId;
                RoomName = snapshot.RoomName;
                Capacity = snapshot.Capacity;
                NowPlaying = snapshot.NowPlaying;
                GameMode = Enum.TryParse(snapshot.GameMode, true, out EGameMode mode) ? mode : EGameMode.Unknown;
                Map = Enum.TryParse(snapshot.Map, true, out EMap map) ? map : EMap.Unknown;
                Players.Clear();

                foreach (var player in snapshot.Players)
                {
                    if (player != null)
                    {
                        Players[player.Id] = player;
                    }
                }
            }
        }
    }
}

