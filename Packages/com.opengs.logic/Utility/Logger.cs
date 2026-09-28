#nullable enable
using System;

namespace OpenGSCore
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception? ex = null);
        void Info(string message) => Log(LogLevel.Info, message);
        void Warning(string message) => Log(LogLevel.Warning, message);
        void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
        void Debug(string message) => Log(LogLevel.Debug, message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(LogLevel level, string message, Exception? ex = null)
        {
            var color = level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Debug => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpper()}] {message}");
            if (ex != null)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ForegroundColor = originalColor;
        }
    }
}

