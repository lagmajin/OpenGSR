#nullable enable
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public sealed class RoomListEntry
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public int Capacity { get; set; } = 8;
        public string GameMode { get; set; } = EGameMode.DeathMatch.ToString();
        public bool TeamBalance { get; set; } = true;
        public int PlayerCount { get; set; } = 0;

        public JObject ToJson()
        {
            return new JObject
            {
                ["RoomId"] = RoomId,
                ["RoomID"] = RoomId,
                ["RoomName"] = RoomName,
                ["OwnerId"] = OwnerId,
                ["OwnerID"] = OwnerId,
                ["Capacity"] = Capacity,
                ["GameMode"] = GameMode,
                ["TeamBalance"] = TeamBalance,
                ["PlayerCount"] = PlayerCount
            };
        }

        public static RoomListEntry FromJson(JObject json)
        {
            if (json == null)
            {
                return new RoomListEntry();
            }

            return new RoomListEntry
            {
                RoomId = json.GetStringAny("RoomId", "RoomID") ?? string.Empty,
                RoomName = json.GetStringAny("RoomName") ?? string.Empty,
                OwnerId = json.GetStringAny("OwnerId", "OwnerID") ?? string.Empty,
                Capacity = json.GetIntAny("Capacity") ?? 8,
                GameMode = json.GetStringAny("GameMode") ?? EGameMode.DeathMatch.ToString(),
                TeamBalance = json.GetBoolAny("TeamBalance") ?? true,
                PlayerCount = json.GetIntAny("PlayerCount", "Players") ?? 0
            };
        }
    }

    public sealed class RoomListSnapshot
    {
        public List<RoomListEntry> Rooms { get; } = new();

        public JObject ToJson(string? messageType = null)
        {
            return new JObject
            {
                ["MessageType"] = messageType ?? MessageType.RoomListUpdateNotification,
                ["Rooms"] = new JArray(Rooms.Select(room => room.ToJson()))
            };
        }

        public JArray ToRoomArray()
        {
            return new JArray(Rooms.Select(room => room.ToJson()));
        }

        public static RoomListSnapshot FromJson(JObject json)
        {
            var snapshot = new RoomListSnapshot();
            if (json == null)
            {
                return snapshot;
            }

            if (json["Rooms"] is JArray rooms)
            {
                foreach (var token in rooms)
                {
                    if (token is not JObject roomJson)
                    {
                        continue;
                    }

                    snapshot.Rooms.Add(RoomListEntry.FromJson(roomJson));
                }
            }

            return snapshot;
        }
    }
}
