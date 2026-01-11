namespace Lexus2_0.Core.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception? exception = null);
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception? exception = null);
        void Critical(string message, Exception? exception = null);
    }
}

