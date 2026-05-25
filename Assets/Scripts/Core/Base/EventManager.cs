using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace OpenGS
{
    class EventManager : SingletonMonoBehaviour<EventManager>
    {
        private bool threadEnd = false;
        private readonly List<OpenGSBaseClass> classess_ = new();
        private readonly object classLock = new();

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
            lock (classLock)
            {
                classess_.RemoveAll(item => item == null);
            }
        }

        private void OnDestroy()
        {
            threadEnd = true;
            lock (classLock)
            {
                classess_.Clear();
            }
        }

        void addEventListner(OpenGSBaseClass cl)
        {
            if (cl == null)
            {
                return;
            }

            lock (classLock)
            {
                if (!classess_.Contains(cl))
                {
                    classess_.Add(cl);
                }
            }
        }

        void removeAllListner()
        {
            lock (classLock)
            {
                classess_.Clear();
            }
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
            OpenGSBaseClass[] snapshot;
            lock (classLock)
            {
                snapshot = classess_.ToArray();
            }

            foreach (var listener in snapshot)
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

        public int ListenerCount()
        {
            lock (classLock)
            {
                return classess_.Count;
            }
        }
    }
}
