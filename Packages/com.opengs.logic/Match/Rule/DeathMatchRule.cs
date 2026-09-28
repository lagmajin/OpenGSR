


namespace OpenGSCore
{


    public sealed class DeathMatchRule : AbstractMatchRule
    {
        private int killLimit = 20;

        /// <summary>
        /// The length a free for all match runs on when its setting does not say.
        /// </summary>
        public const int DefaultMatchTimeMsec = 300000;

        /// <summary>
        /// The kill limit a free for all match runs on when its setting does not say.
        /// </summary>
        public const int DefaultKillLimit = 20;

        public DeathMatchRule(int matchTimeMsec = DefaultMatchTimeMsec, int killCondition = DefaultKillLimit) 
            : base(EGameMode.DeathMatch, matchTimeMsec)
        {
            killLimit = killCondition;
        }

        public DeathMatchRule(in DeathMatchSetting setting)
            : base(
                EGameMode.DeathMatch,
                // The minutes are read rather than a length written once in the
                // setting's constructor, because a snapshot is a value later
                // writes do not change.
                setting?.MatchTimeMinutes > 0
                    ? setting.MatchTimeMinutes * 60 * 1000
                    : DefaultMatchTimeMsec)
        {
            killLimit = setting?.WinConditionKill > 0 ? setting.WinConditionKill : DefaultKillLimit;
        }

        /// <summary>
        /// The number of kills that ends the match, for a client showing the
        /// scoreboard the server is playing to.
        /// </summary>
        public int KillLimit => killLimit;

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            // 時間切れ判定
            if (situation.RemainingTimeSec <= 0) return true;

            // 誰かが規定キル数に到達したか
            if (situation.MaxPlayerKillCount >= killLimit) return true;

            return false;
        }
    }
}
