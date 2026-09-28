using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class CaptureTheFlagMatchSituation : AbstractTeamMatchSituation
    {
        public int RedTeamFlagReturn { get; set; } = 0;
        public int BlueTeamFlagReturn { get; set; } = 0;

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

        public void AddFlagReturn(ETeam team)
        {
            switch (team)
            {
                case ETeam.Red:
                    RedTeamFlagReturn++;
                    break;
                case ETeam.Blue:
                    BlueTeamFlagReturn++;
                    break;
            }
        }

        public void AddTeamKill(ETeam team)
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

        public void Reset()
        {
            RedTeamFlagReturn = 0;
            BlueTeamFlagReturn = 0;
            RedTeamFlagCaptures = 0;
            BlueTeamFlagCaptures = 0;
            RedTeamKill = 0;
            BlueTeamKill = 0;
        }
    }
}
