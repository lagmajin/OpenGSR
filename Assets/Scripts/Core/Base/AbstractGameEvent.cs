using System;
using UnityEngine;


namespace OpenGS
{


    public class AbstractGameEvent : IEvent
    {
        private DateTime timestamp = DateTime.Now;
        private DateTime timestampUtc = DateTime.UtcNow;

        private String EventNameString;

        public string EventName { get => EventNameString; set => EventNameString = value; }
        public DateTime Timestamp { get => timestamp; set => timestamp = value; }
        public DateTime TimestampUts { get => timestampUtc; set => timestampUtc = value; }

        public AbstractGameEvent()
        {

        }

    }

    public static class AbstractEventExt
    {
        public static Type GetType(this AbstractGameEvent e)
        {

            return e.GetType();
        }
    }



}
