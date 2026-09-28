using System;

namespace OpenGSCore
{
    public class TimeLimit
    {
        public long LimitMsec { get; private set; }
        public bool HasTimeLimit { get; private set; }

        public TimeLimit(long limitMsec = 0)
        {
            if (limitMsec > 0) { LimitMsec = limitMsec; HasTimeLimit = true; }
            else { LimitMsec = 0; HasTimeLimit = false; }
        }

        public void SetTimeLimit(long limitMsec)
        {
            if (limitMsec > 0) { LimitMsec = limitMsec; HasTimeLimit = true; }
            else { NoTimeLimit(); }
        }

        public void NoTimeLimit() { LimitMsec = 0; HasTimeLimit = false; }

        public bool IsTimeUp(long elapsedMsec) => HasTimeLimit && elapsedMsec >= LimitMsec;

        public long RemainingMsec(long elapsedMsec)
        {
            if (!HasTimeLimit) return long.MaxValue;
            var rem = LimitMsec - elapsedMsec;
            return rem < 0 ? 0 : rem;
        }
    }
}
