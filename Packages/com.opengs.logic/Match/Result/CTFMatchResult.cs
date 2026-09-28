#nullable enable
namespace OpenGSCore
{
    public class CTFMatchResult : AbstractMatchResult
    {
        private readonly CTFMatchFinalScore finalScore;

        public CTFMatchResult() : this(new CTFMatchFinalScore())
        {
        }

        public CTFMatchResult(CTFMatchFinalScore? score)
        {
            finalScore = score ?? new CTFMatchFinalScore();
            SetOutcome(true, false);
        }

        public CTFMatchFinalScore FinalScore()
        {
            return finalScore;
        }
    }
}

