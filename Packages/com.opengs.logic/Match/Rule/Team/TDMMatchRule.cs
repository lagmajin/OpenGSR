


namespace OpenGSCore
{
    public sealed class TDMMatchRule : AbstractMatchRule
    {
        private int teamKillLimit = 50; // デフォルト50キル

        public TDMMatchRule(int killLimit = 50, int matchTimeMsec = 600000) 
            : base(EGameMode.TeamDeathMatch, matchTimeMsec)
        {
            teamKillLimit = killLimit;
        }

        public TDMMatchRule(in TDMMatchSetting setting) 
            : base(EGameMode.TeamDeathMatch, 600000) // デフォルト10分
        {
            teamKillLimit = 50;
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
