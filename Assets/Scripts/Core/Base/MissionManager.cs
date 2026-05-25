using System.Collections.Generic;

namespace OpenGS
{
    internal class MissionManager
    {
        public static MissionManager Instance { get; } = new();

        private readonly HashSet<MissionMainScript> subscribers = new();

        private MissionManager()
        {
        }

        public void Subscribe(MissionMainScript script)
        {
            if (script == null)
            {
                return;
            }

            subscribers.Add(script);
        }

        public void UnSubscribe(MissionMainScript script)
        {
            if (script == null)
            {
                return;
            }

            subscribers.Remove(script);
        }
    }
}
