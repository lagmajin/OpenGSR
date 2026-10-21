using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class OneShotKillMatchSetting : AbstractMatchSetting
    {
        private int winConditionKill = 1;

        /// <summary>
        /// The number of kills that ends the match. First to one is the mode, but
        /// a room has to be able to ask for a different one.
        /// </summary>
        public int WinConditionKill { get => winConditionKill; set => Set(ref winConditionKill, value); }

        private int matchTimeMinutes = 5;

        /// <summary>
        /// The match length in minutes.
        /// <para>
        /// This setting had no length at all, and the rule for this mode was
        /// built with only a kill limit, so it took the five minute default of an
        /// unrelated rule. A mode nobody gave a length ran on another mode's.
        /// </para>
        /// </summary>
        public int MatchTimeMinutes { get => matchTimeMinutes; set => Set(ref matchTimeMinutes, value); }

        public OneShotKillMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.OneShotKill, maxPlayerCapacity, teamBalance)
        {
            WinConditionKill = 1;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "OneShotKill";
            result["WinConditionKill"] = WinConditionKill;
            result["MatchTimeMinutes"] = MatchTimeMinutes;
            result["Description"] = "First kill wins";

            return result;
        }
    }
}

