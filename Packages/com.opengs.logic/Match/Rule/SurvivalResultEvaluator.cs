using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace OpenGSCore
{
    /// <summary>
    /// サバイバル（個人戦）の勝敗判定ロジック
    /// <para>
    /// A survival match is won by being the last one standing, or by the clock
    /// running out with the score deciding it. The rule is what ends the match,
    /// and this is what says who it ended for, so the two have to agree on the
    /// order: a player who is still alive outranks one who is not however many
    /// kills either has, and among the living the kills decide.
    /// </para>
    /// <para>
    /// It used to sort by health and take whoever was first, which is the same
    /// order, but it declared a winner even when there was none to declare: a room
    /// where everybody was dead, or a room with one player in it, still produced a
    /// winner. A match nobody won is not a win for whoever happened to be at the
    /// top of a list, and the player it named was credited with a win they did not
    /// take.
    /// </para>
    /// </summary>
    public class SurvivalResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var resultJson = new JObject();
            resultJson["MessageType"] = MessageType.MatchEndNotification;

            var safePlayers = (players ?? new List<PlayerInfo>())
                .Where(p => p != null)
                .ToList();

            var survivors = safePlayers.Where(p => p.Health > 0).ToList();
            var ranked = survivors
                .OrderByDescending(p => p.Kills)
                .ThenBy(p => p.Deaths)
                .ThenBy(p => p.Id, System.StringComparer.Ordinal)
                .ToList();

            // Nobody left standing is not a win for the first name in a list. It
            // is a match with no winner, and saying so is the difference between
            // a result and a guess.
            var winner = ranked.FirstOrDefault();
            var isDraw = winner == null;

            resultJson["WinningPlayerId"] = isDraw ? "None" : winner!.Id;
            resultJson["WinnerName"] = isDraw ? "None" : winner.Name;
            resultJson["WinningTeam"] = "None"; // 個人戦なので
            resultJson["IsDraw"] = isDraw;
            resultJson["SurvivorCount"] = survivors.Count;

            var playersArray = new JArray();
            foreach (var p in ranked)
            {
                playersArray.Add(p.ToJson());
            }

            // Everyone who is out goes after the ones still in, so a reader is
            // not left wondering where they went.
            foreach (var p in safePlayers.Except(ranked))
            {
                playersArray.Add(p.ToJson());
            }

            resultJson["Players"] = playersArray;

            return resultJson;
        }
    }
}
