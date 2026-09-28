using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// 待機室の共通スナップショット DTO。
    /// サーバーの RoomInfo とクライアントの表示状態を共通化するための軽量モデル。
    /// </summary>
    public class WaitRoomSnapshot
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int Capacity { get; set; } = 8;
        public bool NowPlaying { get; set; }
        public string GameMode { get; set; } = EGameMode.DeathMatch.ToString();
        public string Map { get; set; } = EMap.Unknown.ToString();
        public bool TeamBalance { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public List<PlayerInfo> Players { get; set; } = new();

        public JObject ToJson()
        {
            var result = new JObject
            {
                ["RoomId"] = RoomId,
                ["RoomID"] = RoomId,
                ["RoomName"] = RoomName,
                ["Capacity"] = Capacity,
                ["NowPlaying"] = NowPlaying,
                ["GameMode"] = GameMode,
                ["Map"] = Map,
                ["TeamBalance"] = TeamBalance,
                ["OwnerId"] = OwnerId,
                ["OwnerID"] = OwnerId
            };

            var players = new JArray();
            foreach (var player in Players)
            {
                if (player != null)
                {
                    players.Add(player.ToJson());
                }
            }

            result["Players"] = players;
            return result;
        }

        public JObject ToNetworkJson(string messageType)
        {
            var result = ToJson();
            result["MessageType"] = messageType;
            return result;
        }

        public static WaitRoomSnapshot FromJson(JObject json)
        {
            if (json == null)
            {
                return new WaitRoomSnapshot();
            }

            var snapshot = new WaitRoomSnapshot
            {
                RoomId = json.GetStringAny("RoomId", "RoomID") ?? string.Empty,
                RoomName = json.GetStringAny("RoomName") ?? string.Empty,
                Capacity = json.GetIntAny("Capacity") ?? 8,
                NowPlaying = json.GetBoolAny("NowPlaying") ?? false,
                GameMode = json.GetStringAny("GameMode") ?? EGameMode.DeathMatch.ToString(),
                Map = json.GetStringAny("Map") ?? EMap.Unknown.ToString(),
                TeamBalance = json.GetBoolAny("TeamBalance") ?? false,
                OwnerId = json.GetStringAny("OwnerId", "OwnerID") ?? string.Empty
            };

            if (json["Players"] is JArray players)
            {
                foreach (var token in players)
                {
                    if (token is not JObject playerJson)
                    {
                        continue;
                    }

                    snapshot.Players.Add(PlayerInfo.FromJson(playerJson));
                }
            }

            return snapshot;
        }
    }
}
