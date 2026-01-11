using System;
using System.IO;
using System.Threading.Tasks;

namespace Lexus2_0.Core.Logging
{
    /// <summary>
    /// File-based logger with rotation and crash recovery
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logDirectory;
        private readonly string _logFile;
        private readonly object _lockObject = new object();
        private const int MaxLogFileSize = 10 * 1024 * 1024; // 10MB

        public FileLogger()
        {
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lexus2.0", "Logs");
            _logFile = Path.Combine(_logDirectory, $"lexus_{DateTime.Now:yyyyMMdd}.log");

            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            var logEntry = FormatLogEntry(level, message, exception);
            
            lock (_lockObject)
            {
                try
                {
                    // Rotate log file if too large
                    if (File.Exists(_logFile) && new FileInfo(_logFile).Length > MaxLogFileSize)
                    {
                        RotateLogFile();
                    }

                    File.AppendAllText(_logFile, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Fail silently - logging should not crash the application
                }
            }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
        public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);

        private string FormatLogEntry(LogLevel level, string message, Exception? exception)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpper().PadRight(8);
            var entry = $"[{timestamp}] [{levelStr}] {message}";

            if (exception != null)
            {
                entry += $"{Environment.NewLine}Exception: {exception.GetType().Name}";
                entry += $"{Environment.NewLine}Message: {exception.Message}";
                entry += $"{Environment.NewLine}Stack Trace: {exception.StackTrace}";
            }

            return entry;
        }

        private void RotateLogFile()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var rotatedFile = Path.Combine(_logDirectory, $"lexus_{timestamp}.log");
            File.Move(_logFile, rotatedFile);
        }

        /// <summary>
        /// Get recent log entries for crash recovery analysis
        /// </summary>
        public string[] GetRecentLogs(int lines = 100)
        {
            if (!File.Exists(_logFile))
                return Array.Empty<string>();

            try
            {
                var allLines = File.ReadAllLines(_logFile);
                var startIndex = Math.Max(0, allLines.Length - lines);
                return allLines[startIndex..];
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}

