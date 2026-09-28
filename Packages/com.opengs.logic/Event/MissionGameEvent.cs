namespace OpenGSCore
{
    public class MissionGateEvent : AbstractGameEvent
    {
        public string GateId { get; set; } = string.Empty;

        public MissionGateEvent()
        {
            EventName = "MissionGateEvent";
        }
    }

    public class MissionSomeoneDead : AbstractGameEvent
    {
        public string PlayerId { get; set; } = string.Empty;

        public MissionSomeoneDead()
        {
            EventName = "MissionSomeoneDead";
        }
    }
}
