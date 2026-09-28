namespace OpenGSCore
{
    public class DeathMatchEvent
    {
        public string PlayerId { get; set; } = string.Empty;
        public int KillCount { get; set; } = 0;
        public int DeathCount { get; set; } = 0;

        public DeathMatchEvent()
        {
        }

        public DeathMatchEvent(string playerId, int killCount, int deathCount)
        {
            PlayerId = playerId ?? string.Empty;
            KillCount = killCount;
            DeathCount = deathCount;
        }
    }
}
