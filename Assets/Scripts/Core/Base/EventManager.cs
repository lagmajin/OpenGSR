using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace OpenGS
{
    class EventManager : SingletonMonoBehaviour<EventManager>
    {
        private bool threadEnd = false;
        private readonly List<OpenGSBaseClass> classess_ = new();

        EventManager()
        {
            ThreadPool.QueueUserWorkItem(threadFunc);
        }

        private void threadFunc(object state)
        {
            while (!threadEnd)
            {
                Thread.Sleep(3000);
            }
        }

        private void Update()
        {
        }

        void addEventListner(OpenGSBaseClass cl)
        {
            if (cl == null)
            {
                return;
            }

            if (!classess_.Contains(cl))
            {
                classess_.Add(cl);
            }
        }

        void removeAllListner()
        {
            classess_.Clear();
        }

        public void Register(OpenGSBaseClass cl)
        {
            addEventListner(cl);
        }

        public void Clear()
        {
            removeAllListner();
        }

        public void sendEvent(AbstractGameEvent ev)
        {
            Debug.Log($"[EventManager] sendEvent: {ev?.EventName ?? "null"}");
            foreach (var listener in classess_)
            {
                listener?.OnOriginalEvent(ev);
            }
        }

        public void sendEvent()
        {
            Debug.Log("[EventManager] sendEvent");
        }

        public void postEvent(AbstractGameEvent ev)
        {
            Debug.Log($"[EventManager] postEvent: {ev?.EventName ?? "null"}");
            sendEvent(ev);
        }

        public void postEvent()
        {
            Debug.Log("[EventManager] postEvent");
        }
    }
}
