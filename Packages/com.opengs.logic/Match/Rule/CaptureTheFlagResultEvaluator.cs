using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// キャプチャー・ザ・フラッグの勝敗判定ロジック
    /// </summary>
    public class CaptureTheFlagResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var resultJson = new JObject();
            resultJson["MessageType"] = MessageType.MatchEndNotification;

            string winningTeam = "Draw";
            if (situation is AbstractTeamMatchSituation teamSituation)
            {
                if (teamSituation.RedTeamFlagCaptures > teamSituation.BlueTeamFlagCaptures)
                {
                    winningTeam = "Red";
                }
                else if (teamSituation.BlueTeamFlagCaptures > teamSituation.RedTeamFlagCaptures)
                {
                    winningTeam = "Blue";
                }
                // フラグ数が同じならキル数で判定（タイブレーカー）
                else if (teamSituation.RedTeamKill > teamSituation.BlueTeamKill)
                {
                    winningTeam = "Red";
                }
                else if (teamSituation.BlueTeamKill > teamSituation.RedTeamKill)
                {
                    winningTeam = "Blue";
                }

                resultJson["RedTeamScore"] = teamSituation.RedTeamFlagCaptures;
                resultJson["BlueTeamScore"] = teamSituation.BlueTeamFlagCaptures;
                resultJson["RedTeamKills"] = teamSituation.RedTeamKill;
                resultJson["BlueTeamKills"] = teamSituation.BlueTeamKill;
            }

            resultJson["WinningTeam"] = winningTeam;

            // プレイヤーごとの戦績
            var playersArray = new JArray();
            foreach (var p in players ?? new List<PlayerInfo>())
            {
                playersArray.Add(p.ToJson());
            }
            resultJson["Players"] = playersArray;

            return resultJson;
        }
    }
}
