using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class TeamSurvivalMatchSituation : AbstractTeamMatchSituation
    {
        /// <summary>
        /// How many players on a team are still in the match.
        /// <para>
        /// Written by the room as players die, because the team survival rule
        /// ends a match when one side is wiped and read these from here. Nothing
        /// wrote them, so the wipe could never happen and that mode could only
        /// ever end on the clock.
        /// </para>
        /// </summary>
        public int RedTeamAliveCount { get; set; } = 0;
        public int BlueTeamAliveCount { get; set; } = 0;

        // The remaining lives that used to sit here had no reader and no writer.
        // A mode where dying is permanent has no lives to be spent, so the fields
        // described a mechanic the rules deliberately do not have.

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
