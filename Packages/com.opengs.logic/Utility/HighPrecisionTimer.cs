using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore.Utility
{
    internal class HighPrecisionTimer
    {
#if WINDOWS
    [DllImport("kernel32.dll")]
    private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    [DllImport("kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(out long lpFrequency);

    private static readonly long frequency;

    static HighPrecisionTimer()
    {
        QueryPerformanceFrequency(out frequency);
    }

    public static double GetTimestamp()
    {
        QueryPerformanceCounter(out long counter);
        return (double)counter / frequency; // 秒単位
    }
#elif LINUX
    [StructLayout(LayoutKind.Sequential)]
    struct timespec
    {
        public long tv_sec;  // 秒
        public long tv_nsec; // ナノ秒
    }

    [DllImport("libc.so.6", EntryPoint = "clock_gettime")]
    private static extern int clock_gettime(int clk_id, out timespec tp);

    private const int CLOCK_MONOTONIC_RAW = 4; // TSC ベースの高精度タイマー

    public static double GetTimestamp()
    {
        clock_gettime(CLOCK_MONOTONIC_RAW, out timespec tp);
        return tp.tv_sec + tp.tv_nsec / 1e9; // 秒単位
    }
#else
        public static double GetTimestamp()
        {
            return DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond; // 最低限の代替策
        }
#endif
    }
}

