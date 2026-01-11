using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexus2_0.Core.Logging
{
    /// <summary>
    /// In-memory logger that stores logs for API retrieval
    /// </summary>
    public class InMemoryLogger : ILogger
    {
        private readonly List<LogEntry> _logs;
        private readonly object _lockObject = new object();
        private const int MaxLogs = 1000;

        public InMemoryLogger()
        {
            _logs = new List<LogEntry>();
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            var logMessage = message;
            if (exception != null)
            {
                logMessage += $": {exception.Message}";
                if (exception.StackTrace != null)
                {
                    logMessage += $"\n{exception.StackTrace}";
                }
            }
            AddLog(level.ToString().ToUpper(), logMessage);
        }

        public void Debug(string message)
        {
            AddLog("DEBUG", message);
        }

        public void Info(string message)
        {
            AddLog("INFO", message);
        }

        public void Warning(string message)
        {
            AddLog("WARNING", message);
        }

        public void Error(string message, Exception? ex = null)
        {
            var errorMessage = message;
            if (ex != null)
            {
                errorMessage += $": {ex.Message}";
                if (ex.StackTrace != null)
                {
                    errorMessage += $"\n{ex.StackTrace}";
                }
            }
            AddLog("ERROR", errorMessage);
        }

        public void Critical(string message, Exception? ex = null)
        {
            var criticalMessage = message;
            if (ex != null)
            {
                criticalMessage += $": {ex.Message}";
                if (ex.StackTrace != null)
                {
                    criticalMessage += $"\n{ex.StackTrace}";
                }
            }
            AddLog("CRITICAL", criticalMessage);
        }

        private void AddLog(string level, string message)
        {
            lock (_lockObject)
            {
                _logs.Add(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = message
                });

                // Keep only the last MaxLogs entries
                if (_logs.Count > MaxLogs)
                {
                    _logs.RemoveRange(0, _logs.Count - MaxLogs);
                }
            }
        }

        public List<LogEntry> GetLogs(int? limit = null)
        {
            lock (_lockObject)
            {
                var logs = _logs.OrderByDescending(l => l.Timestamp).ToList();
                if (limit.HasValue && limit.Value > 0)
                {
                    logs = logs.Take(limit.Value).ToList();
                }
                return logs.OrderBy(l => l.Timestamp).ToList(); // Return in chronological order
            }
        }

        public void ClearLogs()
        {
            lock (_lockObject)
            {
                _logs.Clear();
            }
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

