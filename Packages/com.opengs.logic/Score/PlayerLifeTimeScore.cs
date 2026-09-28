

namespace OpenGSCore
{
    public class PlayerLifeTimeScore
    {
        public int TotalMatchCount { get; private set; } = 0;
        public int DeathMatchCount { get; private set; } = 0;
        public int DeathMatchWinCount { get; private set; } = 0;
        public int DeathMatchLoseCount { get; set; } = 0;
        public int TeamDeathMatchWinCount { get; set; } = 0;
        public int TeamDeathMatchLoseCount { get; set; } = 0;
        public int SurvivalWinCount { get; set; } = 0;
        public int SurvivalLoseCount { get; set; } = 0;
        public int TeamSurvivalWinCount { get; set; } = 0;
        public int TeamSurvivalLoseCount { get; set; } = 0;
        public int CtfFlagReturn { get; set; } = 0;
        public int CtfFlagInterrupt { get; set; } = 0;


        public PlayerLifeTimeScore()
        {

        }

        public void IncrementTotalMatchCount()
        {
            TotalMatchCount++;
        }

        public void IncrementTeamDeathMatchWinCount()
        {
            TeamDeathMatchWinCount++;
            
        }

        public void IncrementSurvivalWinCount()
        {
            SurvivalWinCount++;
        }

        public void IncrementTeamSurvivalWinCount()
        {
            TeamSurvivalWinCount++;
        }
        
        public void RecordDeathMatchResult(bool won)
        {
            IncrementTotalMatchCount();
            DeathMatchCount++;
            if (won)
            {
                DeathMatchWinCount++;
            }
            else
            {
                DeathMatchLoseCount++;
            }
        }

        public void RecordTeamDeathMatchResult(bool won)
        {
            IncrementTotalMatchCount();
            if (won)
            {
                TeamDeathMatchWinCount++;
            }
            else
            {
                TeamDeathMatchLoseCount++;
            }
        }

        public void RecordSurvivalResult(bool won, bool isTeamMatch)
        {
            IncrementTotalMatchCount();
            if (isTeamMatch)
            {
                if (won)
                {
                    TeamSurvivalWinCount++;
                }
                else
                {
                    TeamSurvivalLoseCount++;
                }
            }
            else
            {
                if (won)
                {
                    SurvivalWinCount++;
                }
                else
                {
                    SurvivalLoseCount++;
                }
            }
        }

        public void RecordCtfFlagReturn()
        {
            CtfFlagReturn++;
        }

        public void RecordCtfFlagInterrupt()
        {
            CtfFlagInterrupt++;
        }



    }
}
