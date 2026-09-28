#nullable enable
namespace OpenGSCore
{
    public class DeathMatchResult : AbstractMatchResult
    {
        private readonly DeathMatchFinalScore finalScore;

        public DeathMatchResult() : this(new DeathMatchFinalScore())
        {
        }

        public DeathMatchResult(DeathMatchFinalScore? score)
        {
            finalScore = score ?? new DeathMatchFinalScore();
            SetOutcome(true, false);
        }

        public DeathMatchFinalScore FinalScore()
        {
            return finalScore;
        }
    }
}

