
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

        /// <summary>
        /// The number of captures that ends the match.
        /// <para>
        /// Read by a client that has to show the same scoreboard the server is
        /// playing to. It was private with no reader, so a client carried its own
        /// number and a scoreboard could say first to five on a match the server
        /// ends at three.
        /// </para>
        /// </summary>
        public int FlagLimit => flagLimit;

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
