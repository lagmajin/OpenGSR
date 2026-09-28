


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
