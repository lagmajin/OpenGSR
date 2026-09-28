


namespace OpenGSCore
{

    public interface IAbstractMatchSituation
    {


    }
    public class AbstractMatchSituation : IAbstractMatchSituation
    {
        public EGameMode mode;

        public int AlivePlayers { get; set; } = 0;
        public float RemainingTimeSec { get; set; } = 3600; // デフォルト 1時間

        public int TotalKill { get; set; } = 0;
        public int TotalDeath { get; set; } = 0;

        public int MaxPlayerKillCount { get; set; } = 0;

        /// <summary>
        /// Records a kill, and keeps the highest single tally in the match.
        /// <para>
        /// The rules decide a match on the best kill count, so somebody has to
        /// write it. The recorder lived on the death match's own situation, and
        /// the rooms for death match and survival were both given the base one,
        /// so nothing in the server ever called it and the kill condition in
        /// those rules could not fire at all: they could only ever end on the
        /// clock. It is here because it is a fact about any match.
        /// </para>
        /// </summary>
        public void RecordKill(int playerKillCount)
        {
            TotalKill++;
            if (playerKillCount > MaxPlayerKillCount)
            {
                MaxPlayerKillCount = playerKillCount;
            }
        }

        public void RecordDeath()
        {
            TotalDeath++;
        }

        public void UpdateTime(float deltaTime)
        {
            RemainingTimeSec -= deltaTime;
            if (RemainingTimeSec < 0) RemainingTimeSec = 0;
        }
    }


    public class AbstractTeamMatchSituation : AbstractMatchSituation
    {
        public int RedTeamKill { get; set; } = 0;
        public int BlueTeamKill { get; set; } = 0;
        public int RedTeamFlagCaptures { get; set; } = 0;
        public int BlueTeamFlagCaptures { get; set; } = 0;
    }

}
