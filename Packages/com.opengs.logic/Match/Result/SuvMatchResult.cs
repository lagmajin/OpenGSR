#nullable enable
namespace OpenGSCore
{
    public class SuvMatchFinalScore
    {
    }

    public class SuvMatchResult : AbstractMatchResult
    {
        private readonly SuvMatchFinalScore finalScore;

        public SuvMatchResult() : this(new SuvMatchFinalScore())
        {
        }

        public SuvMatchResult(SuvMatchFinalScore? score)
        {
            finalScore = score ?? new SuvMatchFinalScore();
            SetOutcome(true, false);
        }

        public SuvMatchFinalScore FinalScore()
        {
            return finalScore;
        }
    }
}

