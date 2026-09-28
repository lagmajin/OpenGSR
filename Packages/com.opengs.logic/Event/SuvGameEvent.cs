namespace OpenGSCore
{
    public class SuvGameEvent : AbstractGameEvent
    {
        public string PlayerId { get; set; } = string.Empty;
        public int AliveCount { get; set; } = 0;

        public SuvGameEvent()
        {
            EventName = "SuvGameEvent";
        }
    }
}
