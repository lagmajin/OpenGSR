using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGS
{
    class EventManager:SingletonMonoBehaviour<EventManager>
    {
        bool threadEnd = false;

        List<OpenGSBaseClass> classess_;


        EventManager()
        {
            ThreadPool.QueueUserWorkItem(threadFunc);
        }

        private void threadFunc(object state)
        {
            while(true)
            {
               Task.Delay(3000);

            }

        }

        private void Update()
        {
            
        }

        void addEventListner(OpenGSBaseClass cl)
        {

        }

        void removeAllListner()
        {

        }

        public void sendEvent()
        {

        }

        public void postEvent()
        {

        }

    }
}
