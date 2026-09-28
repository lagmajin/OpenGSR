
namespace OpenGSCore
{
    /// <summary>
    /// チームサバイバルのルール
    /// <para>
    /// A team survival match ends either when one side is wiped out or when the
    /// clock runs out, and which of those a room is playing is the setting's
    /// choice. That choice used to be a field nothing read, so every team
    /// survival match was played as last team standing whatever it was configured
    /// for, and a room set up to be decided on the clock would end the moment one
    /// team was down to its last player.
    /// </para>
    /// </summary>
    public sealed class TSuvMatchRule : AbstractMatchRule
    {
        /// <summary>
        /// Whether a wipe ends this match, rather than the clock.
        /// </summary>
        public bool LastTeamStanding { get; }

        /// <summary>
        /// The clock a team survival match runs on when the setting does not say.
        /// </summary>
        public const int DefaultSurvivalTimeMinutes = 10;

        public TSuvMatchRule(int matchTimeMsec = 600000)
            : this(matchTimeMsec, lastTeamStanding: true)
        {
        }

        public TSuvMatchRule(int matchTimeMsec, bool lastTeamStanding)
            : base(EGameMode.TeamSurvival, matchTimeMsec)
        {
            LastTeamStanding = lastTeamStanding;
        }

        public TSuvMatchRule(in TeamSurvivalMatchSetting setting)
            : this(
                // A setting with no time on it is a match that has already
                // ended, so the default is the only better answer than taking a
                // zero as a duration.
                setting?.SurvivalTimeMinutes > 0
                    ? setting.SurvivalTimeMinutes * 60 * 1000
                    : DefaultSurvivalTimeMinutes * 60 * 1000,
                setting?.LastTeamStanding ?? true)
        {
        }

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            if (situation == null)
            {
                return true;
            }

            // The clock always ends it, whichever way the match is set to be won.
            if (situation.RemainingTimeSec <= 0)
            {
                return true;
            }

            if (!LastTeamStanding)
            {
                // This match is decided on the clock, so a team being wiped out
                // is a round of it and not the end of it.
                return false;
            }

            // One side gone. The counts are what the room keeps up to date as
            // players die, so this is the room saying one side is finished.
            if (situation is TeamSurvivalMatchSituation tsuvSituation)
            {
                if (tsuvSituation.RedTeamAliveCount <= 0 || tsuvSituation.BlueTeamAliveCount <= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool CanReSpawn()
        {
            // Team Survival は一度死ぬとリスポーン不可
            return false;
        }
    }
}
