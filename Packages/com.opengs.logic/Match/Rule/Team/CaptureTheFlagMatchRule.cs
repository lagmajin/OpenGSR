
using System;


namespace OpenGSCore
{
    public sealed class CaptureTheFlagMatchRule : AbstractMatchRule
    {
        private int flagLimit = 3; // 3回フラッグ得点で勝利

        public CaptureTheFlagMatchRule(int limit = 3, int matchTimeMsec = 600000) 
            : base(EGameMode.CaptureTheFlag, matchTimeMsec)
        {
            flagLimit = limit;
        }

        public override bool IsMatchFinished(AbstractMatchSituation situation)
        {
            // 時間切れ判定
            if (situation.RemainingTimeSec <= 0) return true;

            // 旗の得点数判定
            if (situation is CaptureTheFlagMatchSituation ctfSituation)
            {
                if (ctfSituation.RedTeamFlagCaptures >= flagLimit || ctfSituation.BlueTeamFlagCaptures >= flagLimit)
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
