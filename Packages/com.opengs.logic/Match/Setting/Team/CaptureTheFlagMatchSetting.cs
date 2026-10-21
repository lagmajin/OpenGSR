using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    public class CaptureTheFlagMatchSetting : AbstractMatchSetting
    {
        private int winConditionPoint = 3;
        public int WinConditionPoint => winConditionPoint;

        /// <summary>
        /// The match length in minutes.
        /// <para>
        /// This setting had no length at all, so the rule fell back to its own ten
        /// minute default and a room could not ask for a capture the flag of any
        /// other length. Death match and team death match both carry one; this is
        /// the same fact about this mode, and its absence is what made the factory
        /// hand the rule only a point limit.
        /// </para>
        /// <para>
        /// Minutes rather than milliseconds, because that is what the other rules
        /// read and a value written once in a constructor is a value later writes
        /// cannot change.
        /// </para>
        /// </summary>
        private int matchTimeMinutes = 10;

        /// <summary>
        /// The match length, frozen with the rest of the settings once the room has
        /// started the match on it.
        /// </summary>
        public int MatchTimeMinutes { get => matchTimeMinutes; set => Set(ref matchTimeMinutes, value); }

        public CaptureTheFlagMatchSetting(int winCondition = 3, bool teamBalance = true, int matchTimeMinutes = 10)
            : base(EGameMode.CaptureTheFlag, 8, teamBalance, true)
        {
            winConditionPoint = winCondition;
            MatchTimeMinutes = matchTimeMinutes;
            MatchTimeMSec = MatchTimeMinutes * 60 * 1000;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();
            result["MatchType"] = "CaptureTheFlag";
            result["WinConditionPoint"] = WinConditionPoint;
            result["MatchTimeMinutes"] = MatchTimeMinutes;
            result["TeamBalance"] = true;
            return result;
        }
    }
}