using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public interface IAbstractGameEvent
    {

    }
    public class AbstractGameEvent
    {
        private DateTime timestampUtc = DateTime.UtcNow;
        public string EventName { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DateTime TimestampUts { get => timestampUtc; set => timestampUtc = value; }

        public AbstractGameEvent()
        {

        }
    }



}
