using Newtonsoft.Json.Linq;

namespace OpenGSCore
{
    /// <summary>
    /// A mission match.
    /// <para>
    /// This setting used to construct itself as a survival match. Every room
    /// created through it was therefore played by the survival rule and reported
    /// as one, so a mission was a survival match with a mission shaped name on it
    /// and its own fields, life count and objective count, went to nobody.
    /// </para>
    /// <para>
    /// The mode is the one the setting declares. Nothing here is a fallback for
    /// another mode: a mission is played by the mission rule and settled by the
    /// mission evaluator, which is what makes its own numbers mean anything.
    /// </para>
    /// </summary>
    public class MissionMatchSetting : AbstractMatchSetting
    {
        private string missionType = "Default";
        private int objectiveCount = 1;
        private bool allowRespawn = false;
        private int lifeCount = 3;
        private int matchTimeMinutes = 10;

        /// <summary>Which mission this is. Free form so a mission can be named.</summary>
        public string MissionType { get => missionType; set => Set(ref missionType, value); }

        /// <summary>
        /// How much of the objective has to be done.
        /// <para>
        /// It was a field nobody read. The mission rule ends on it, so a mission
        /// with a count of five is five of something rather than one.
        /// </para>
        /// </summary>
        public int ObjectiveCount { get => objectiveCount; set => Set(ref objectiveCount, value); }

        /// <summary>
        /// Whether a player who goes down comes back.
        /// <para>
        /// A mission is usually played on a life count, so this defaults to false
        /// and the life count is the thing that decides how many goes there are.
        /// </para>
        /// </summary>
        public bool AllowRespawn { get => allowRespawn; set => Set(ref allowRespawn, value); }

        /// <summary>
        /// How many times the party may go down.
        /// <para>
        /// This lived on the mission rule as a private number and on the player as
        /// a field neither of them wrote. It is here so a room can ask for a
        /// different mission, and it is what the rule ends the match on.
        /// </para>
        /// </summary>
        public int LifeCount { get => lifeCount; set => Set(ref lifeCount, value); }

        /// <summary>
        /// The match length in minutes, on the same terms as every other mode: a
        /// room has to be able to ask for a length, and the rule reads this.
        /// </summary>
        public int MatchTimeMinutes { get => matchTimeMinutes; set => Set(ref matchTimeMinutes, value); }

        public MissionMatchSetting(
            int maxPlayerCapacity = 4,
            string missionType = "Default",
            EGameMode mode = EGameMode.Mission)
            : base(mode, maxPlayerCapacity, false, true)
        {
            MissionType = missionType;
            AllowRespawn = false;
            LifeCount = 3;
            MatchTimeMSec = MatchTimeMinutes * 60 * 1000;
        }

        public override JObject ToJson()
        {
            var result = base.ToJson();

            result["MatchType"] = "Mission";
            result["MissionType"] = MissionType;
            result["ObjectiveCount"] = ObjectiveCount;
            result["AllowRespawn"] = AllowRespawn;
            result["LifeCount"] = LifeCount;
            result["MatchTimeMinutes"] = MatchTimeMinutes;

            return result;
        }
    }
}