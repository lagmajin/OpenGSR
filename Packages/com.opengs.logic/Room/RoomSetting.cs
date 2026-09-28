using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// 待機室・ルーム設定の共通DTO。
    /// 既存の WaitRoom / MatchSetting に渡す前の編集用データをまとめる。
    /// </summary>
    public class RoomSetting
    {
        public string RoomName { get; set; } = string.Empty;
        public int Capacity { get; set; } = 8;
        public EGameMode GameMode { get; set; } = EGameMode.DeathMatch;
        public bool TeamBalance { get; set; } = true;
        public string Map { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool HasRoomName { get; internal set; }
        public bool HasCapacity { get; internal set; }
        public bool HasGameMode { get; internal set; }
        public bool HasTeamBalance { get; internal set; }
        public bool HasMap { get; internal set; }
        public bool HasPasswordValue { get; internal set; }

        public int MaxPlayerCount
        {
            get => Capacity;
            set => Capacity = value;
        }

        public bool HasPassword => !string.IsNullOrWhiteSpace(Password);

        public RoomSetting()
        {
        }

        public RoomSetting(string roomName, int capacity = 8, EGameMode gameMode = EGameMode.DeathMatch)
        {
            RoomName = roomName;
            Capacity = capacity;
            GameMode = gameMode;
            HasRoomName = true;
            HasCapacity = true;
            HasGameMode = true;
        }

        public virtual void Normalize()
        {
            RoomName = RoomName?.Trim() ?? string.Empty;
            Capacity = Math.Max(1, Capacity);
            if (!Enum.IsDefined(typeof(EGameMode), GameMode))
            {
                GameMode = EGameMode.DeathMatch;
            }
        }

        public virtual void ApplyTo(WaitRoom room)
        {
            if (room == null)
            {
                return;
            }

            Normalize();

            if (HasRoomName)
            {
                room.SetRoomName(RoomName);
            }

            if (HasGameMode && GameMode != EGameMode.Unknown)
            {
                room.ChangeGameMode(GameMode);
            }

            if (HasMap && Enum.TryParse<EMap>(Map, true, out var parsedMap))
            {
                room.Map = parsedMap;
            }
            else if (HasGameMode && GameMode != EGameMode.Unknown)
            {
                // The request named no map, or named one that does not exist. The
                // room has to be given a map it can be played on, and which maps
                // those are is a question about the mode. Without this a room kept
                // an unknown map and the client fell back to one of its own
                // choosing, which for capture the flag meant a stage with no flag
                // stands: a match that could not be won by anybody.
                var playable = OpenGSCore.GameMode.DefaultMapFor(GameMode);
                if (playable.HasValue)
                {
                    room.Map = playable.Value;
                }
            }

            if (HasPasswordValue)
            {
                room.Password = Password ?? string.Empty;
            }

            if (HasCapacity)
            {
                room.SetRoomCapacity(Capacity);
                if (room.setting != null)
                {
                    room.setting.MaxPlayerCount = Capacity;
                }
            }

            if (HasTeamBalance && room.setting is AbstractTeamMatchSetting teamSetting)
            {
                teamSetting.TeamBalance = TeamBalance;
            }
        }

        public virtual JObject ToJson()
        {
            Normalize();

            return new JObject
            {
                ["RoomName"] = RoomName,
                ["Capacity"] = Capacity,
                ["GameMode"] = GameMode.ToString(),
                ["TeamBalance"] = TeamBalance,
                ["Map"] = Map,
                ["HasPassword"] = HasPassword,
            };
        }

        public static RoomSetting FromJson(JToken? token)
        {
            var setting = new RoomSetting();
            if (token == null)
            {
                return setting;
            }

            var source = token as JObject ?? JObject.FromObject(token);
            var nested = source["Settings"] as JObject;
            if (nested != null)
            {
                source = nested;
            }

            if (source.Property("RoomName") != null || source.Property("RoomTitle") != null)
            {
                setting.RoomName = source["RoomName"]?.ToString() ?? source["RoomTitle"]?.ToString() ?? string.Empty;
                setting.HasRoomName = true;
            }

            if (source.Property("Capacity") != null && int.TryParse(source["Capacity"]?.ToString(), out var capacity))
            {
                setting.Capacity = capacity;
                setting.HasCapacity = true;
            }

            var gameModeRaw = source["GameMode"]?.ToString() ?? string.Empty;
            if (source.Property("GameMode") != null && Enum.TryParse<EGameMode>(gameModeRaw, true, out var parsedMode))
            {
                setting.GameMode = parsedMode;
                setting.HasGameMode = true;
            }

            if (source.Property("TeamBalance") != null && bool.TryParse(source["TeamBalance"]?.ToString(), out var teamBalance))
            {
                setting.TeamBalance = teamBalance;
                setting.HasTeamBalance = true;
            }

            if (source.Property("Map") != null)
            {
                setting.Map = source["Map"]?.ToString() ?? string.Empty;
                setting.HasMap = true;
            }

            if (source.Property("Password") != null)
            {
                setting.Password = source["Password"]?.ToString() ?? string.Empty;
                setting.HasPasswordValue = true;
            }
            setting.Normalize();
            return setting;
        }

        public static RoomSetting FromDictionary(IDictionary<string, JToken> dic)
        {
            if (dic == null)
            {
                return new RoomSetting();
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
