using Newtonsoft.Json.Linq;
using System;

namespace OpenGSCore
{
    public class PlayerResult
    {
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Score { get; set; }
        public bool IsWinner { get; set; }
        public string Team { get; set; } = "None";
        public string ResultMessage { get; set; } = string.Empty;

        public PlayerResult()
        {
        }

        public PlayerResult(string playerId, string playerName, int kills = 0, int deaths = 0, int score = 0)
        {
            PlayerId = playerId ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            Kills = kills;
            Deaths = deaths;
            Score = score;
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["PlayerId"] = PlayerId,
                ["PlayerName"] = PlayerName,
                ["Kills"] = Kills,
                ["Deaths"] = Deaths,
                ["Score"] = Score,
                ["IsWinner"] = IsWinner,
                ["Team"] = Team,
                ["ResultMessage"] = ResultMessage
            };
        }

        public static PlayerResult FromJson(JObject json)
        {
            if (json == null)
            {
                return new PlayerResult();
            }

            return new PlayerResult
            {
                PlayerId = json["PlayerId"]?.ToString() ?? string.Empty,
                PlayerName = json["PlayerName"]?.ToString() ?? string.Empty,
                Kills = json["Kills"]?.ToObject<int>() ?? 0,
                Deaths = json["Deaths"]?.ToObject<int>() ?? 0,
                Score = json["Score"]?.ToObject<int>() ?? 0,
                IsWinner = json["IsWinner"]?.ToObject<bool>() ?? false,
                Team = json["Team"]?.ToString() ?? "None",
                ResultMessage = json["ResultMessage"]?.ToString() ?? string.Empty
            };
        }
    }
}
