using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// サバイバル（個人戦）の勝敗判定ロジック
    /// </summary>
    public class SurvivalResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var resultJson = new JObject();
            resultJson["MessageType"] = MessageType.MatchEndNotification;

            var sortedPlayers = (players ?? new List<PlayerInfo>())
                .OrderByDescending(p => p.Health > 0)
                .ThenByDescending(p => p.Kills)
                .ThenBy(p => p.Deaths)
                .ToList();

            string winnerId = sortedPlayers.Count > 0 ? sortedPlayers[0].Id : "None";
            string winnerName = sortedPlayers.Count > 0 ? sortedPlayers[0].Name : "None";

            resultJson["WinningPlayerId"] = winnerId;
            resultJson["WinnerName"] = winnerName;
            resultJson["WinningTeam"] = "None"; // 個人戦なので

            var playersArray = new JArray();
            foreach (var p in sortedPlayers)
            {
                playersArray.Add(p.ToJson());
            }
            resultJson["Players"] = playersArray;

            return resultJson;
        }
    }
}
