
namespace OpenGSCore
{
    public sealed class TSuvMatchRule : AbstractMatchRule
    {
        public TSuvMatchRule(int matchTimeMsec = 600000) 
            : base(EGameMode.TeamSurvival, matchTimeMsec)
        {
        }

        public TSuvMatchRule(in TeamSurvivalMatchSetting setting)
            : base(EGameMode.TeamSurvival, (setting?.SurvivalTimeMinutes ?? 10) * 60 * 1000)
        {
        }

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            // 時間切れ判定
            if (situation.RemainingTimeSec <= 0) return true;

            // チームの生存者数判定
            if (situation is TeamSurvivalMatchSituation tsuvSituation)
            {
                // ゲームが開始しており、かつどちらかのチームが全滅した場合
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
