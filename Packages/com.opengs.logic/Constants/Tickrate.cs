using System;

namespace OpenGSCore
{
    public static class Tickrate
    {
        public const int Tickrate10 = 100;
        public const int Tickrate25 = 40;
        public const int Tickrate50 = 20;
        public const int Tickrate100 = 10;
        public const int DefaultTickrate = Tickrate25;

        public static int CalcSleepTime(int tickrate)
        {
            if (tickrate <= 0) return DefaultTickrate;
            return 1000 / tickrate;
        }
    }
}
