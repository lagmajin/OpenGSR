using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class SuvMatchSetting : AbstractMatchSetting
    {
        /// <summary>
        /// The clock a survival match runs on when nothing else says.
        /// <para>
        /// This setting used to leave MatchTimeMSec at its default of zero, and
        /// the rule reads the clock to decide the match is over. So a survival
        /// room started with no time on it at all and reported itself finished
        /// the first time it was looked at, before a shot had been fired. A
        /// mode where a death is permanent needs a clock to be the one that ends
        /// the match, so it has to have one.
        /// </para>
        /// </summary>
        public const int DefaultSurvivalTimeMinutes = 10;

        public int SurvivalTimeMinutes { get; set; } = DefaultSurvivalTimeMinutes;
        public int WinConditionKill { get; set; } = 1;
        public float HealthMultiplier { get; set; } = 2.0f;

        public SuvMatchSetting(int maxPlayer, bool teamBalance)
            : base(EGameMode.Survival, maxPlayer, teamBalance)
        {
            TimeLimit = false;
            AllowOvertime = false;
            MatchTimeMSec = SurvivalTimeMinutes * 60 * 1000;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "Survival";
            result["SurvivalTimeMinutes"] = SurvivalTimeMinutes;
            result["WinConditionKill"] = WinConditionKill;
            result["HealthMultiplier"] = HealthMultiplier;
            result["TeamBalance"] = false;
            return result;
        }
    }
}
