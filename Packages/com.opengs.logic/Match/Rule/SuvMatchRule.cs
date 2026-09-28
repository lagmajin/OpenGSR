


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
                // The minutes are read rather than a snapshot of them. A setting
                // wrote MatchTimeMSec once in its constructor, so a room
                // configured for a different length kept the default and the
                // change was silently ignored.
                setting?.SurvivalTimeMinutes > 0
                    ? setting.SurvivalTimeMinutes * 60 * 1000
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
