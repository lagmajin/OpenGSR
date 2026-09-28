using OpenGSCore;

namespace OpenGSServer
{
    public class MatchResultFactory
    {
        public static AbstractMatchResult CreateMatchResult(AbstractFinalScore score)
        {
            return MatchResultResolver.Create(score);
        }
    }
}
