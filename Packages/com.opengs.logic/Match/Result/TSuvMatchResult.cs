#nullable enable
namespace OpenGSCore
{
    public class TeamSuvMatchFinalScore
    {
    }

    public class TSuvMatchResult : AbstractMatchResult
    {
        private readonly TeamSuvMatchFinalScore finalScore;

        public TSuvMatchResult() : this(new TeamSuvMatchFinalScore())
        {
        }

        public TSuvMatchResult(TeamSuvMatchFinalScore? score)
        {
            finalScore = score ?? new TeamSuvMatchFinalScore();
            SetOutcome(true, false);
        }

        public TeamSuvMatchFinalScore FinalScore()
        {
            return finalScore;
        }
    }
}

