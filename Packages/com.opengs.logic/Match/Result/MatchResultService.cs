namespace OpenGSCore
{
    public class MatchResultService
    {
        public AbstractMatchResult CreateMatchResult(AbstractMatchFinalScore score)
        {
            return MatchResultResolver.Create(score);
        }

        public AbstractMatchResult createMatchResult(AbstractMatchFinalScore score) => CreateMatchResult(score);
    }
}
