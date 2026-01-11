using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Lexus2_0.Core.Models;
using Newtonsoft.Json;

namespace Lexus2_0.Core.Config
{
    /// <summary>
    /// Manages configuration storage with encryption support
    /// </summary>
    public class ConfigManager
    {
        private readonly string _configDirectory;
        private readonly string _configFile;
        private readonly string _encryptedConfigFile;

        public ConfigManager()
        {
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lexus2.0");
            _configFile = Path.Combine(_configDirectory, "config.json");
            _encryptedConfigFile = Path.Combine(_configDirectory, "config.encrypted");

            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        /// <summary>
        /// Save configuration to JSON file (unencrypted)
        /// </summary>
        public void SaveConfig(BookingConfig config, bool encrypt = false)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);

            if (encrypt)
            {
                var encrypted = EncryptString(json);
                File.WriteAllText(_encryptedConfigFile, encrypted);
            }
            else
            {
                File.WriteAllText(_configFile, json);
            }
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public BookingConfig? LoadConfig(bool encrypted = false)
        {
            try
            {
                string json;
                
                if (encrypted && File.Exists(_encryptedConfigFile))
                {
                    var encryptedData = File.ReadAllText(_encryptedConfigFile);
                    json = DecryptString(encryptedData);
                }
                else if (File.Exists(_configFile))
                {
                    json = File.ReadAllText(_configFile);
                }
                else
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<BookingConfig>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Encrypt sensitive data (simplified - use proper key management in production)
        /// </summary>
        private string EncryptString(string plainText)
        {
            // Note: In production, use proper key management (DPAPI, Azure Key Vault, etc.)
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }

        /// <summary>
        /// Decrypt sensitive data
        /// </summary>
        private string DecryptString(string encryptedText)
        {
            var bytes = Convert.FromBase64String(encryptedText);
            var unprotectedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unprotectedBytes);
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool ValidateConfig(BookingConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.TrainNo))
                return false;
            if (string.IsNullOrWhiteSpace(config.SourceStation))
                return false;
            if (string.IsNullOrWhiteSpace(config.DestinationStation))
                return false;
            if (string.IsNullOrWhiteSpace(config.TravelDate))
                return false;
            if (string.IsNullOrWhiteSpace(config.Username))
                return false;
            if (string.IsNullOrWhiteSpace(config.Password))
                return false;
            if (config.PassengerDetails == null || config.PassengerDetails.Count == 0)
                return false;

            return true;
        }
    }
}

