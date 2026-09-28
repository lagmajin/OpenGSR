using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// デスマッチの勝敗判定ロジック
    /// (最高キル数のプレイヤーが勝者)
    /// </summary>
    public class DeathMatchResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var resultJson = new JObject();
            resultJson["MessageType"] = MessageType.MatchEndNotification;
            resultJson["WinningTeam"] = "None";

            var safePlayers = players ?? new List<PlayerInfo>();
            if (!safePlayers.Any())
            {
                resultJson["Winner"] = "NoPlayers";
                resultJson["Players"] = new JArray();
                return resultJson;
            }

            var topScore = safePlayers.Max(p => p?.Kills ?? 0);
            var topPlayers = safePlayers.Where(p => (p?.Kills ?? 0) == topScore).ToList();
            var winner = topPlayers.Count == 1 ? topPlayers[0] : null;

            resultJson["Winner"] = winner != null ? winner.Id : "Draw";
            resultJson["WinningPlayerId"] = winner != null ? winner.Id : "Draw";
            resultJson["WinnerName"] = winner != null ? winner.Name : "Draw";
            resultJson["TopKills"] = topScore;

            var playersArray = new JArray();
            foreach (var p in safePlayers.OrderByDescending(p => p.Kills).ThenBy(p => p.Deaths))
            {
                playersArray.Add(p.ToJson());
            }
            resultJson["Players"] = playersArray;

            return resultJson;
        }
    }
}
