using OpenGSCore;

namespace OpenGS
{
    public interface IMatchRule
    {
        bool D(in MatchData d);
    }
}
