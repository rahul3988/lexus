using System.IO;

namespace Lexus2_0.Desktop.Utils
{
    /// <summary>
    /// Manages folder structure for logs, captchas, cache, etc.
    /// </summary>
    public static class FolderManager
    {
        private static readonly string BasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Lexus2.0"
        );

        public static string LogsPath { get; private set; } = string.Empty;
        public static string WebLogsPath { get; private set; } = string.Empty;
        public static string FailedCaptchaPath { get; private set; } = string.Empty;
        public static string CachePath { get; private set; } = string.Empty;

        static FolderManager()
        {
            InitializeFolders();
        }

        public static void InitializeFolders()
        {
            LogsPath = Path.Combine(BasePath, "Logs");
            WebLogsPath = Path.Combine(BasePath, "Web_Logs");
            FailedCaptchaPath = Path.Combine(BasePath, "Failed_Captcha");
            CachePath = Path.Combine(BasePath, "Cache");

            // Create all folders if they don't exist
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(WebLogsPath);
            Directory.CreateDirectory(FailedCaptchaPath);
            Directory.CreateDirectory(CachePath);
        }

        public static string GetFailedCaptchaPath(string ticketId, string filename)
        {
            var ticketFolder = Path.Combine(FailedCaptchaPath, ticketId);
            Directory.CreateDirectory(ticketFolder);
            return Path.Combine(ticketFolder, filename);
        }
    }
}

