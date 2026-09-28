using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    public class RoomCapacity
    {
        public int Min { get; set; } = 1;
        public int Max { get; set; } = 16;
        public int Current { get; set; } = 8;

        public RoomCapacity()
        {
            Current = 8;
        }

        public RoomCapacity(int current, int min = 1, int max = 16)
        {
            Min = min;
            Max = max;
            Current = Clamp(current);
        }

        public int Clamp(int value)
        {
            return Math.Max(Min, Math.Min(Max, value));
        }

        public void Set(int value)
        {
            Current = Clamp(value);
        }

    }
}

