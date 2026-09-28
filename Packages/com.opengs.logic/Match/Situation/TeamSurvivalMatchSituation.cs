using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class TeamSurvivalMatchSituation : AbstractTeamMatchSituation
    {
        public int RedTeamAliveCount { get; set; } = 0;
        public int BlueTeamAliveCount { get; set; } = 0;
        
        // 残機合計（オプション）
        public int RedTeamTotalLives { get; set; } = 0;
        public int BlueTeamTotalLives { get; set; } = 0;

        public void SetAliveCount(ETeam team, int count)
        {
            count = Math.Max(0, count);
            switch (team)
            {
                case ETeam.Red:
                    RedTeamAliveCount = count;
                    AlivePlayers = RedTeamAliveCount + BlueTeamAliveCount;
                    break;
                case ETeam.Blue:
                    BlueTeamAliveCount = count;
                    AlivePlayers = RedTeamAliveCount + BlueTeamAliveCount;
                    break;
            }
        }

        public void AddKill(ETeam team)
        {
            switch (team)
            {
                case ETeam.Red:
                    RedTeamKill++;
                    break;
                case ETeam.Blue:
                    BlueTeamKill++;
                    break;
            }
        }

        public void AddFlagCapture(ETeam team)
        {
            switch (team)
            {
                case ETeam.Red:
                    RedTeamFlagCaptures++;
                    break;
                case ETeam.Blue:
                    BlueTeamFlagCaptures++;
                    break;
            }
        }
    }
}
