using System.Collections.Generic;
using System.Linq;

namespace OpenGS
{
    internal class MissionManager
    {
        public static MissionManager Instance { get; } = new();

        private readonly HashSet<MissionMainScript> subscribers = new();
        private readonly object subscriberLock = new();

        private MissionManager()
        {
        }

        public void Subscribe(MissionMainScript script)
        {
            if (script == null)
            {
                return;
            }

            lock (subscriberLock)
            {
                subscribers.Add(script);
            }
        }

        public void UnSubscribe(MissionMainScript script)
        {
            if (script == null)
            {
                return;
            }

            lock (subscriberLock)
            {
                subscribers.Remove(script);
            }
        }

        public void Publish(AbstractGameEvent ev)
        {
            MissionMainScript[] snapshot;
            lock (subscriberLock)
            {
                snapshot = subscribers.ToArray();
            }

            foreach (var subscriber in snapshot)
            {
                subscriber?.PostEvent(ev);
            }
        }

        public int Count()
        {
            lock (subscriberLock)
            {
                return subscribers.Count;
            }
        }

        public void Clear()
        {
            lock (subscriberLock)
            {
                subscribers.Clear();
            }
        }
    }
}
