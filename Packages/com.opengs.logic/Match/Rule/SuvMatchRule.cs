


namespace OpenGSCore
{
    public sealed class SuvMatchRule : AbstractMatchRule
    {
        private int winConditionKill = 1;

        public SuvMatchRule() : base(EGameMode.Survival)
        {
        }

        public SuvMatchRule(in SuvMatchSetting setting)
            : base(
                EGameMode.Survival,
                // A setting with no time on it is a match that has already ended,
                // so the room is given the default rather than the zero it was
                // handed. The clock is what ends a survival match when nobody
                // reaches the kill condition, so it cannot be allowed to arrive
                // as nothing.
                setting?.MatchTimeMSec > 0
                    ? setting.MatchTimeMSec
                    : SuvMatchSetting.DefaultSurvivalTimeMinutes * 60 * 1000)
        {
            winConditionKill = setting?.WinConditionKill ?? 1;
        }

        /// <summary>
        /// The number of kills that ends the match.
        /// <para>
        /// Read by a client that has to show the same target the server is playing
        /// to, and by the room state that publishes it.
        /// </para>
        /// </summary>
        public int WinConditionKill => winConditionKill;

        public override bool CanReSpawn()
        {
            return false;
        }

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            if (situation == null)
            {
                return true;
            }

            if (situation.RemainingTimeSec <= 0)
            {
                return true;
            }

            return situation.MaxPlayerKillCount >= winConditionKill;
        }
    }
}
