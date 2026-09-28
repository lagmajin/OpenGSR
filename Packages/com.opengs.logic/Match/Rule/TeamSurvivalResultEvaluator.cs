using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// チームサバイバルの勝敗判定ロジック
    /// </summary>
    public class TeamSurvivalResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var resultJson = new JObject();
            resultJson["MessageType"] = MessageType.MatchEndNotification;

            var safePlayers = players ?? new List<PlayerInfo>();

            // 各チームの生存者数をカウント
            int redAlive = safePlayers.Count(p => p.Team == ETeam.Red && p.Health > 0);
            int blueAlive = safePlayers.Count(p => p.Team == ETeam.Blue && p.Health > 0);

            string winningTeam = "Draw";
            if (redAlive > blueAlive)
            {
                winningTeam = "Red";
            }
            else if (blueAlive > redAlive)
            {
                winningTeam = "Blue";
            }
            else
            {
                // 生存数が同じならキル数などで判定
                int redKills = safePlayers.Where(p => p.Team == ETeam.Red).Sum(p => p.Kills);
                int blueKills = safePlayers.Where(p => p.Team == ETeam.Blue).Sum(p => p.Kills);

                if (redKills > blueKills) winningTeam = "Red";
                else if (blueKills > redKills) winningTeam = "Blue";
            }

            resultJson["WinningTeam"] = winningTeam;
            resultJson["RedAliveCount"] = redAlive;
            resultJson["BlueAliveCount"] = blueAlive;

            var playersArray = new JArray();
            foreach (var p in safePlayers)
            {
                playersArray.Add(p.ToJson());
            }
            resultJson["Players"] = playersArray;

            return resultJson;
        }
    }
}
