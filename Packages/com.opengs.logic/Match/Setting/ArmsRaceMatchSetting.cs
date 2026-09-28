using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class ArmsRaceMatchSetting : AbstractMatchSetting
    {
        /// <summary>
        /// The number of kills that ends the match. Settable, because a room has
        /// to be able to ask for a different one than the default.
        /// </summary>
        public int WinConditionKill { get; set; } = 30;

        /// <summary>
        /// The match length in minutes.
        /// <para>
        /// This setting had no length at all, and the rule for this mode was
        /// built with only a kill limit, so it took the five minute default of an
        /// unrelated rule.
        /// </para>
        /// </summary>
        public int MatchTimeMinutes { get; set; } = 5;

        public ArmsRaceMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.ArmsRace, maxPlayerCapacity, teamBalance)
        {
            WinConditionKill = 30;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "ArmsRace";
            result["WinConditionKill"] = WinConditionKill;
            result["MatchTimeMinutes"] = MatchTimeMinutes;
            result["Description"] = "Collect weapons and eliminate enemies";

            return result;
        }
    }
}
