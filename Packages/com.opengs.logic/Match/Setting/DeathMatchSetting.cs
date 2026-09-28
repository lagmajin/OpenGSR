using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class DeathMatchSetting : AbstractMatchSetting
    {
        /// <summary>
        /// The number of kills that ends the match.
        /// <para>
        /// Settable, because a room has to be able to ask for a different match
        /// than the default. It was a private field behind a getter, so nothing
        /// outside the constructor could change it and a "first to thirty" room
        /// could not be created.
        /// </para>
        /// </summary>
        public int WinConditionKill { get; set; } = 20;

        /// <summary>
        /// The match length in minutes.
        /// <para>
        /// None of the free for all settings had a length at all, and the rules
        /// fell back to five minutes because the value they read was always
        /// zero. A fallback standing in for a setting is a match nobody chose.
        /// </para>
        /// <para>
        /// The length in milliseconds is derived from this rather than stored,
        /// because a value written once in a constructor is a value that later
        /// writes do not change, and the rule reads the minutes anyway.
        /// </para>
        /// </summary>
        public int MatchTimeMinutes { get; set; } = 5;

        public DeathMatchSetting(int winConditionKill = 20, bool teamBalance = true)
            : base(EGameMode.DeathMatch, 0, teamBalance)
        {
            WinConditionKill = winConditionKill;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "DeathMatch";
            result["WinConditionKill"] = WinConditionKill;
            result["MatchTimeMinutes"] = MatchTimeMinutes;
            result["TeamBalance"] = "true";

            return result;

        }
    }
}
