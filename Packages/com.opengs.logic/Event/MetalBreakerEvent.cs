namespace OpenGSCore
{
    public class MetalBreakerEnemyAllDeadEvent
    {
        public string MissionId { get; set; } = string.Empty;
        public int RemainingEnemies { get; set; } = 0;

        public MetalBreakerEnemyAllDeadEvent()
        {
        }

        public MetalBreakerEnemyAllDeadEvent(string missionId, int remainingEnemies = 0)
        {
            MissionId = missionId ?? string.Empty;
            RemainingEnemies = remainingEnemies;
        }
    }
}
