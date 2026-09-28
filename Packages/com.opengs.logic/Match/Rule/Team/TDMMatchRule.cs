


namespace OpenGSCore
{
    public sealed class TDMMatchRule : AbstractMatchRule
    {
        private int teamKillLimit = 50; // デフォルト50キル

        /// <summary>
        /// The number of team kills that ends the match.
        /// <para>
        /// Read by a client showing the same scoreboard the server is playing to,
        /// and published in the room state with the rest of the settings.
        /// </para>
        /// </summary>
        public int TeamKillLimit => teamKillLimit;

        /// <summary>
        /// The clock a team death match runs on when the setting does not say.
        /// </summary>
        public const int DefaultMatchTimeMsec = 600000;

        /// <summary>
        /// The kill limit a team death match runs on when the setting does not say.
        /// </summary>
        public const int DefaultTeamKillLimit = 50;

        public TDMMatchRule(int matchTimeMsec = DefaultMatchTimeMsec, int killLimit = DefaultTeamKillLimit)
            : base(EGameMode.TeamDeathMatch, matchTimeMsec)
        {
            teamKillLimit = killLimit;
        }

        public TDMMatchRule(in TDMMatchSetting setting)
            : this(
                // The minutes are read rather than the snapshot of them, because a
                // snapshot is a value later writes do not change: a room
                // configured for seven minutes would have kept the ten its
                // constructor wrote. This threw the setting away entirely, which
                // is how it got away for so long.
                setting?.MatchTimeMinutes > 0
                    ? setting.MatchTimeMinutes * 60 * 1000
                    : DefaultMatchTimeMsec,
                setting?.WinConditionKill > 0 ? setting.WinConditionKill : DefaultTeamKillLimit)
        {
        }

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            // 時間切れ判定
            if (situation.RemainingTimeSec <= 0) return true;

            // チームスコア判定
            if (situation is AbstractTeamMatchSituation teamSituation)
            {
                if (teamSituation.RedTeamKill >= teamKillLimit || teamSituation.BlueTeamKill >= teamKillLimit)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool CanReSpawn()
        {
            return true;
        }
    }
}
