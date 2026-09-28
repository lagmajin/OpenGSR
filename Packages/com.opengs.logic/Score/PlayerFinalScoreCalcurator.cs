using System;
using System;



namespace OpenGSCore
{
    public interface IPlayerFinalScoreCalculator
    {
    }

    public interface IPlayerFinalScoreCalcurator : IPlayerFinalScoreCalculator
    {
    }
    /// <summary>
    /// スコアの計算を担当するクラス。  
    /// This class handles score calculations.
    /// </summary>
    /// #Score
    public class PlayerFinalScoreCalculator : IPlayerFinalScoreCalculator
    {
        public PlayerFinalScoreCalculator() { }

        public static void CalcScore(AbstractPlayerMatchFinalScore finalScore)
        {
            if (finalScore == null)
            {
                return;
            }

            switch (finalScore.Mode)
            {
                case EGameMode.DeathMatch:
                    if (finalScore is PlayerDeathMatchFinalScore deathMatchScore)
                    {
                        deathMatchScore.CalcTotalPoint();
                    }

                    break;

                case EGameMode.TeamDeathMatch:
                    if (finalScore is PlayerTeamDeathMatchFinalScore teamDeathMatchScore)
                    {
                        teamDeathMatchScore.CalcTotalPoint();
                    }

                    break;

                case EGameMode.CaptureTheFlag:
                    if (finalScore is PlayerCTFMatchFinalScore ctfMatchScore)
                    {
                        ctfMatchScore.CalcTotalPoint();
                    }

                    break;

                default:
                    finalScore.CalcTotalPoint();
                    break;
            }
        }
    }

    [System.Obsolete("Use PlayerFinalScoreCalculator instead.")]
    public class PlayerFinalScoreCalcurator : PlayerFinalScoreCalculator, IPlayerFinalScoreCalcurator
    {
        public PlayerFinalScoreCalcurator() { }

        [System.Obsolete("Use PlayerFinalScoreCalculator.CalcScore instead.")]
        public static void calcScore(AbstractPlayerMatchFinalScore finalScore)
        {
            CalcScore(finalScore);
        }
    }

}
