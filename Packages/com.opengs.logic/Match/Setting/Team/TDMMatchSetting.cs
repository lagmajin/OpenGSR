using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class TDMMatchSetting : AbstractTeamMatchSetting
    {
        /// <summary>
        /// The kill limit a team death match runs on, and the clock it runs on.
        /// <para>
        /// Neither existed here, so the rule took a hardcoded ten minutes and a
        /// hardcoded fifty kills and threw this setting away. A room configured
        /// for a different match was played on numbers nobody had asked for.
        /// </para>
        /// </summary>
        public int WinConditionKill { get; set; } = TDMMatchRule.DefaultTeamKillLimit;

        /// <summary>
        /// The match length in minutes.
        /// </summary>
        public int MatchTimeMinutes { get; set; } = 10;

        public TDMMatchSetting(int maxPlayerCapacity = 8, bool teamBalance = true)
            : base(EGameMode.TeamDeathMatch, true, teamBalance)
        {
            MaxPlayerCount = maxPlayerCapacity;

            // A default so a caller reading it is not told the match has no length.
            // The rule reads the minutes rather than this, because a value written
            // once in a constructor is a value that later writes do not change.
            MatchTimeMSec = 10 * 60 * 1000;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "TeamDeathMatch";
            result["MaxPlayerCount"] = MaxPlayerCount;
            result["WinConditionKill"] = WinConditionKill;
            result["MatchTimeMinutes"] = MatchTimeMinutes;
            return result;
        }
    }
}
