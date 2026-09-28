

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
        public int CtfWinCount { get; set; } = 0;
        public int CtfLoseCount { get; set; } = 0;


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

        /// <summary>
        /// Records a win or a loss in a capture the flag match.
        /// <para>
        /// A capture the flag result used to fall into the death match branch,
        /// because there was no case for it. A player's lifetime record then
        /// counted their capture the flag wins as death match wins, which is a
        /// number they can see and a number that is simply wrong. The mode is
        /// counted the same way the others are: the match happened, and they won
        /// it or they did not.
        /// </para>
        /// </summary>
        public void RecordCaptureTheFlagResult(bool won)
        {
            IncrementTotalMatchCount();
            if (won)
            {
                CtfWinCount++;
            }
            else
            {
                CtfLoseCount++;
            }
        }



    }
}
