using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// ミッション/クエスト結果評価
    /// ライフ残数、スコアに基づいて判定
    /// </summary>
    public class MissionResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            if (players == null || players.Count == 0)
            {
                return CreateFailResult("NoPlayers");
            }

            var player = players.FirstOrDefault(p => !p.IsBot);
            if (player == null)
            {
                player = players.FirstOrDefault();
            }

            int lifeRemaining = player?.LifeCount ?? MissionRuleProvider.CurrentRule?.LifeRemaining ?? 0;
            int score = player?.Score ?? 0;

            bool success = lifeRemaining > 0 && score >= 0;

            var result = new JObject
            {
                ["Success"] = success,
                ["LifeRemaining"] = lifeRemaining,
                ["Score"] = score,
                ["ResultType"] = "Mission"
            };

            if (success)
            {
                result["WinningTeam"] = player?.Team.ToString() ?? "None";
            }

            return result;
        }

        private JObject CreateFailResult(string reason)
        {
            return new JObject
            {
                ["Success"] = false,
                ["LifeRemaining"] = 0,
                ["Score"] = 0,
                ["ResultType"] = "Mission",
                ["Reason"] = reason
            };
        }
    }

    public static class MissionResultEvaluatorFactory
    {
        public static IMatchResultEvaluator CreateEvaluator(EGameMode mode)
        {
            // Missions currently share a single evaluator. The mode parameter is
            // kept so per-mode mission rules can be split out later without
            // changing every caller.
            return new MissionResultEvaluator();
        }
    }

    public static class MissionRuleProvider
    {
        public static MissionRule? CurrentRule { get; set; }
    }
}