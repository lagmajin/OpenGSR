#nullable enable
namespace OpenGSCore
{
    public class TeamDeathMatchResult : AbstractMatchResult
    {
        private readonly TeamDeathMatchFinalScore finalScore;

        public TeamDeathMatchResult() : this(new TeamDeathMatchFinalScore())
        {
        }

        public TeamDeathMatchResult(TeamDeathMatchFinalScore? score)
        {
            finalScore = score ?? new TeamDeathMatchFinalScore();
            SetOutcome(true, false);
        }

        public TeamDeathMatchFinalScore FinalScore()
        {
            return finalScore;
        }
    }
}

