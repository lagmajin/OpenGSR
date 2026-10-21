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

        private int survivalTimeMinutes = DefaultSurvivalTimeMinutes;

        public int SurvivalTimeMinutes { get => survivalTimeMinutes; set => Set(ref survivalTimeMinutes, value); }
        private int winConditionKill = 1;

        public int WinConditionKill { get => winConditionKill; set => Set(ref winConditionKill, value); }
        private float healthMultiplier = 2.0f;

        public float HealthMultiplier { get => healthMultiplier; set => Set(ref healthMultiplier, value); }

        public SuvMatchSetting(int maxPlayer, bool teamBalance)
            : base(EGameMode.Survival, maxPlayer, teamBalance)
        {
            TimeLimit = false;
            AllowOvertime = false;

            // Deliberately not setting MatchTimeMSec here. It is a snapshot, so
            // writing the minutes after the constructor ran would leave the
            // snapshot saying something else and the change would be silently
            // ignored. The rule reads the minutes instead, so there is one number
            // and it is the one that is written.
            MatchTimeMSec = DefaultSurvivalTimeMinutes * 60 * 1000;
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

