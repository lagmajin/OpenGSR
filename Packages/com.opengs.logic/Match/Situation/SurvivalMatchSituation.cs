using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class SurvivalMatchSituation : AbstractMatchSituation
    {
        public int SurvivingPlayers { get; private set; } = 0;

        public void SetSurvivingPlayers(int count)
        {
            SurvivingPlayers = Math.Max(0, count);
            AlivePlayers = SurvivingPlayers;
        }

        public void RecordDeath()
        {
            TotalDeath++;
            if (SurvivingPlayers > 0)
            {
                SurvivingPlayers--;
            }

            AlivePlayers = SurvivingPlayers;
        }
    }
}
