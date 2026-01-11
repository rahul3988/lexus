using System;
using System.IO;
using System.Text.Json;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Services
{
    /// <summary>
    /// Service for persisting and loading bypass settings
    /// </summary>
    public class BypassSettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Lexus2.0",
            "bypass_settings.json"
        );

        public void SaveSettings(BypassSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - settings persistence is not critical
                System.Diagnostics.Debug.WriteLine($"Failed to save bypass settings: {ex.Message}");
            }
        }

        public BypassSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<BypassSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load bypass settings: {ex.Message}");
            }

            // Return default settings if load fails
            return new BypassSettings();
        }
    }
}

