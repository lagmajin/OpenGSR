using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// How a mission ended.
    /// <para>
    /// A mission does not have a winning team. It has an outcome, decided on what
    /// the party achieved and what it had left, and this says which of those
    /// decided it so the result screen and the client's own copy agree.
    /// </para>
    /// <para>
    /// The life count used to be read from a static nobody assigned, so it always
    /// answered zero and every mission failed. It is read from the party now.
    /// </para>
    /// </summary>
    public class MissionResultEvaluator : IMatchResultEvaluator
    {
        public JObject Evaluate(AbstractMatchSituation situation, List<PlayerInfo> players)
        {
            var party = (players ?? new List<PlayerInfo>())
                .Where(player => player != null)
                .ToList();

            var result = new JObject
            {
                ["MessageType"] = MessageType.MatchEndNotification,
                ["ResultType"] = "Mission",
                ["Result"] = "Fail"
            };

            if (party.Count == 0)
            {
                result["Reason"] = "NoPlayers";
                result["Success"] = false;
                return result;
            }

            var lifeRemaining = party.Sum(player => Math.Max(0, player.LifeCount));
            var score = party.Sum(player => Math.Max(0, player.Score));

            result["LifeRemaining"] = lifeRemaining;
            result["Score"] = score;
            result["Kills"] = party.Sum(player => Math.Max(0, player.Kills));
            result["Deaths"] = party.Sum(player => Math.Max(0, player.Deaths));

            var rule = situation as MissionMatchSituation;
            if (rule != null)
            {
                result["ObjectiveCount"] = rule.ObjectiveCount;
                result["ObjectiveProgress"] = rule.ObjectiveProgress;
            }

            // The objective, then the lives. A mission completed is a mission
            // completed whether or not the party got out of it; the lives only
            // decide it when there was nothing left to do.
            var objectiveDone = rule != null && rule.IsObjectiveComplete();
            var outOfLives = lifeRemaining <= 0;

            result["ObjectiveCompleted"] = objectiveDone;
            result["Success"] = objectiveDone || !outOfLives;
            result["Result"] = objectiveDone
                ? "Clear"
                : (outOfLives ? "Fail" : "TimeUp");

            var playersArray = new JArray();
            foreach (var player in party)
            {
                playersArray.Add(player.ToJson());
            }

            result["Players"] = playersArray;
            return result;
        }
    }
}