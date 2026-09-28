




namespace OpenGSCore
{
    public interface IMatchRule
    {
        bool IsMatchFinished(AbstractMatchSituation situation);
        bool CanReSpawn();
        int MatchTimeMSec();
    }

    public abstract class AbstractMatchRule : IMatchRule
    {
        public EGameMode Mode { get; protected set; }
        public int MatchTimeLimitMsec { get; protected set; } = 300000; // デフォルト 5分

        public abstract bool IsMatchFinished(AbstractMatchSituation situation);

        public virtual bool CanReSpawn() => true;

        public AbstractMatchRule(EGameMode mode, int matchTimeMsec = 300000)
        {
            Mode = mode;
            MatchTimeLimitMsec = matchTimeMsec;
        }

        public int MatchTimeMSec() => MatchTimeLimitMsec;
    }



}
