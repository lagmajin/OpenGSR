using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// チームデスマッチの勝敗判定ロジック
    /// </summary>
    public class TeamDeathMatchResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var resultJson = new JObject();
            resultJson["MessageType"] = MessageType.MatchEndNotification;

            var safePlayers = players ?? new List<PlayerInfo>();
            string winningTeam = "Draw";
            if (situation is AbstractTeamMatchSituation teamSit)
            {
                if (teamSit.RedTeamKill > teamSit.BlueTeamKill) winningTeam = "Red";
                else if (teamSit.BlueTeamKill > teamSit.RedTeamKill) winningTeam = "Blue";
                
                resultJson["WinningTeam"] = winningTeam;
                resultJson["RedScore"] = teamSit.RedTeamKill;
                resultJson["BlueScore"] = teamSit.BlueTeamKill;
            }
            else
            {
                resultJson["WinningTeam"] = "Draw";
                resultJson["RedScore"] = 0;
                resultJson["BlueScore"] = 0;
            }

            // 個別プレイヤーデータの追加
            var playersArray = new JArray();
            foreach (var p in safePlayers)
            {
                var pObj = new JObject();
                pObj["PlayerId"] = p.Id;
                pObj["Name"] = p.Name;
                pObj["Team"] = p.Team.ToString();
                pObj["Kills"] = p.Kills;
                pObj["Deaths"] = p.Deaths;
                pObj["Score"] = p.Kills * 100; // 仮のスコア計算
                playersArray.Add(pObj);
            }
            resultJson["Players"] = playersArray;

            return resultJson;
        }
    }
}
