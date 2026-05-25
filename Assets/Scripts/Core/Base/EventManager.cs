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

        public void sendEvent()
        {
            Debug.Log("[EventManager] sendEvent");
        }

        public void postEvent()
        {
            Debug.Log("[EventManager] postEvent");
        }
    }
}
