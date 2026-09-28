


namespace OpenGSCore
{


    public sealed class DeathMatchRule : AbstractMatchRule
    {
        private int killLimit = 20;

        public DeathMatchRule(int killCondition = 20, int matchTimeMsec = 300000) 
            : base(EGameMode.DeathMatch, matchTimeMsec)
        {
            killLimit = killCondition;
        }

        public DeathMatchRule(in DeathMatchSetting setting) 
            : base(EGameMode.DeathMatch, setting?.MatchTimeMSec > 0 ? setting.MatchTimeMSec : 300000)
        {
            killLimit = setting?.WinConditionKill ?? 20;
        }

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
