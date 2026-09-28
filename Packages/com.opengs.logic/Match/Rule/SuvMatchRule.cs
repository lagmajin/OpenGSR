


namespace OpenGSCore
{
    public sealed class SuvMatchRule : AbstractMatchRule
    {
        private int winConditionKill = 1;

        public SuvMatchRule() : base(EGameMode.Survival)
        {
        }

        public SuvMatchRule(in SuvMatchSetting setting) : base(EGameMode.Survival, setting.MatchTimeMSec)
        {
            winConditionKill = setting?.WinConditionKill ?? 1;
        }

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
