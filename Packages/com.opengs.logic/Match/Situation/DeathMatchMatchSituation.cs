using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class DeathMatchMatchSituation : AbstractMatchSituation
    {
        public int TopKills { get; private set; } = 0;

        public void RecordKill(int playerKillCount)
        {
            TotalKill++;
            if (playerKillCount > TopKills)
            {
                TopKills = playerKillCount;
                MaxPlayerKillCount = playerKillCount;
            }
        }

        public void RecordDeath()
        {
            TotalDeath++;
        }
    }
}
