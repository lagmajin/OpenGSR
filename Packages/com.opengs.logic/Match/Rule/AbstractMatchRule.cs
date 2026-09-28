




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

        /// <summary>
        /// How long this match runs for, in milliseconds.
        /// <para>
        /// Zero used to mean "finished", because several settings never wrote a
        /// length and the rules that read one fell back to a default while the
        /// rules that read it directly ended the match on their first look. A
        /// length nobody can express is a match whose length is whatever the
        /// reader decided. This says no length at all, rather than saying zero,
        /// so a rule built without one cannot be mistaken for a match over.
        /// </para>
        /// </summary>
        public int MatchTimeLimitMsec { get; protected set; } = NoTimeLimit;

        /// <summary>
        /// A match with no length. Refused by every rule, so it can only be a
        /// default to be replaced rather than a value to be played.
        /// </summary>
        public const int NoTimeLimit = 0;

        public abstract bool IsMatchFinished(AbstractMatchSituation situation);

        public virtual bool CanReSpawn() => true;

        public AbstractMatchRule(EGameMode mode, int matchTimeMsec = NoTimeLimit)
        {
            Mode = mode;
            MatchTimeLimitMsec = matchTimeMsec;
        }

        public int MatchTimeMSec() => MatchTimeLimitMsec;
    }



}
