using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public sealed class RoomInfoSnapshot
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public int Capacity { get; set; } = 8;
        public string GameMode { get; set; } = EGameMode.DeathMatch.ToString();
        public string Map { get; set; } = string.Empty;
        public bool TeamBalance { get; set; } = true;
        public List<string> BannedWeapons { get; set; } = new List<string>();
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
                ["Map"] = Map,
                ["TeamBalance"] = TeamBalance,
                ["BannedWeapons"] = new JArray(BannedWeapons ?? new List<string>()),
                ["PlayerCount"] = PlayerCount
            };
        }

        public JObject ToResponseJson(string messageType, bool success = true, string errorMessage = null)
        {
            var json = ToJson();
            json["MessageType"] = messageType;
            json["Success"] = success;

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                json["ErrorMessage"] = errorMessage;
            }

            json["RoomInfo"] = ToJson();
            return json;
        }

        public JObject ToNotificationJson(string messageType)
        {
            var json = ToJson();
            json["MessageType"] = messageType;
            json["RoomInfo"] = ToJson();
            return json;
        }

        public static RoomInfoSnapshot FromJson(JObject json)
        {
            if (json == null)
            {
                return new RoomInfoSnapshot();
            }

            var source = json["RoomInfo"] as JObject ?? json;

            return new RoomInfoSnapshot
            {
                RoomId = source.GetStringAny("RoomId", "RoomID") ?? string.Empty,
                RoomName = source.GetStringAny("RoomName") ?? string.Empty,
                OwnerId = source.GetStringAny("OwnerId", "OwnerID") ?? string.Empty,
                Capacity = source.GetIntAny("Capacity") ?? 8,
                GameMode = source.GetStringAny("GameMode") ?? EGameMode.DeathMatch.ToString(),
                Map = source.GetStringAny("Map") ?? string.Empty,
                TeamBalance = source.GetBoolAny("TeamBalance") ?? true,
                BannedWeapons = source["BannedWeapons"] is JArray bannedWeapons
                    ? bannedWeapons.Values<string>().Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    : new List<string>(),
                PlayerCount = source.GetIntAny("PlayerCount", "Players", "WaitingPlayerCount") ?? 0
            };
        }
    }
}
