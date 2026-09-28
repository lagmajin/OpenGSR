using System;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// 待機室の設定変更に使う DTO。
    /// ルーム設定そのものに加えて、ルーム識別用の情報を持つ。
    /// </summary>
    public class WaitRoomSetting : RoomSetting
    {
        public string RoomId { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public int PlayerCount { get; set; } = 0;
        public bool NowPlaying { get; set; } = false;
        public bool HasRoomId { get; private set; }
        public bool HasOwnerId { get; private set; }
        public bool HasPlayerCount { get; private set; }
        public bool HasNowPlaying { get; private set; }

        public WaitRoomSetting()
        {
        }

        public WaitRoomSetting(string roomName, int capacity = 8, EGameMode gameMode = EGameMode.DeathMatch)
            : base(roomName, capacity, gameMode)
        {
        }

        public override void Normalize()
        {
            base.Normalize();
            RoomId = RoomId?.Trim() ?? string.Empty;
            OwnerId = OwnerId?.Trim() ?? string.Empty;
            PlayerCount = Math.Max(0, PlayerCount);
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["RoomId"] = RoomId;
            result["OwnerId"] = OwnerId;
            result["PlayerCount"] = PlayerCount;
            result["NowPlaying"] = NowPlaying;
            return result;
        }

        public static new WaitRoomSetting FromJson(JToken? token)
        {
            var baseSetting = RoomSetting.FromJson(token);
            var setting = new WaitRoomSetting
            {
                RoomName = baseSetting.RoomName,
                Capacity = baseSetting.Capacity,
                GameMode = baseSetting.GameMode,
                TeamBalance = baseSetting.TeamBalance,
                Map = baseSetting.Map,
                Password = baseSetting.Password,
                HasRoomName = baseSetting.HasRoomName,
                HasCapacity = baseSetting.HasCapacity,
                HasGameMode = baseSetting.HasGameMode,
                HasTeamBalance = baseSetting.HasTeamBalance,
                HasMap = baseSetting.HasMap,
                HasPasswordValue = baseSetting.HasPasswordValue
            };

            if (token == null)
            {
                return setting;
            }

            var root = token as JObject ?? JObject.FromObject(token);

            if (root.Property("RoomId") != null || root.Property("RoomID") != null)
            {
                setting.RoomId = root["RoomId"]?.ToString() ?? root["RoomID"]?.ToString() ?? string.Empty;
                setting.HasRoomId = true;
            }

            if (root.Property("OwnerId") != null || root.Property("OwnerID") != null)
            {
                setting.OwnerId = root["OwnerId"]?.ToString() ?? root["OwnerID"]?.ToString() ?? string.Empty;
                setting.HasOwnerId = true;
            }

            if (root.Property("PlayerCount") != null && root["PlayerCount"] != null)
            {
                setting.PlayerCount = root["PlayerCount"]?.ToObject<int>() ?? 0;
                setting.HasPlayerCount = true;
            }

            if (root.Property("NowPlaying") != null && root["NowPlaying"] != null)
            {
                setting.NowPlaying = root["NowPlaying"]?.ToObject<bool>() ?? false;
                setting.HasNowPlaying = true;
            }

            setting.Normalize();
            return setting;
        }

        public static new WaitRoomSetting FromDictionary(System.Collections.Generic.IDictionary<string, JToken> dic)
        {
            if (dic == null)
            {
                return new WaitRoomSetting();
            }

            var json = new JObject();
            foreach (var kv in dic)
            {
                if (kv.Value != null)
                {
                    json[kv.Key] = kv.Value;
                }
            }

            return FromJson(json);
        }
    }
}
