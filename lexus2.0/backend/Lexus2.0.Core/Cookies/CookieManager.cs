using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using Lexus2_0.Core.Logging;

namespace Lexus2_0.Core.Cookies
{
    /// <summary>
    /// Cookie management system (inspired by NeXuS implementation)
    /// Handles IRCTC cookie collection, storage, and restoration
    /// </summary>
    public class CookieManager
    {
        private readonly ILogger _logger;
        private readonly string _cookieDirectory;
        private readonly string _cookieFile;

        public CookieManager(ILogger logger)
        {
            _logger = logger;
            _cookieDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lexus2.0", "Cookies");
            _cookieFile = Path.Combine(_cookieDirectory, "robot.json"); // TeslaX style filename

            if (!Directory.Exists(_cookieDirectory))
            {
                Directory.CreateDirectory(_cookieDirectory);
            }
        }

        /// <summary>
        /// Save cookies to file (similar to NeXuS robot.json)
        /// </summary>
        public void SaveCookies(List<CookieData> cookies)
        {
            try
            {
                var json = JsonSerializer.Serialize(cookies, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_cookieFile, json);
                _logger.Info($"Saved {cookies.Count} cookies to {_cookieFile}");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to save cookies", ex);
                throw;
            }
        }

        /// <summary>
        /// Load cookies from file
        /// </summary>
        public List<CookieData>? LoadCookies()
        {
            try
            {
                if (!File.Exists(_cookieFile))
                {
                    _logger.Debug("No saved cookies found");
                    return null;
                }

                var json = File.ReadAllText(_cookieFile);
                var cookies = JsonSerializer.Deserialize<List<CookieData>>(json);

                if (cookies != null)
                {
                    _logger.Info($"Loaded {cookies.Count} cookies from {_cookieFile}");
                }

                return cookies;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load cookies", ex);
                return null;
            }
        }

        /// <summary>
        /// Clear all IRCTC cookies
        /// </summary>
        public void ClearCookies()
        {
            try
            {
                if (File.Exists(_cookieFile))
                {
                    File.Delete(_cookieFile);
                    _logger.Info("Cookies cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to clear cookies", ex);
            }
        }

        // Note: ToPlaywrightCookies method moved to Automation layer
        // to avoid Playwright dependency in Core

        /// <summary>
        /// Check if cookies are valid (not expired)
        /// </summary>
        public bool AreCookiesValid(List<CookieData>? cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return false;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return cookies.All(c => c.ExpirationDate == 0 || c.ExpirationDate > now);
        }
    }

    /// <summary>
    /// Cookie data structure (compatible with Chrome extension format)
    /// </summary>
    public class CookieData
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string? Path { get; set; }
        public double ExpirationDate { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public string SameSite { get; set; } = "None";
    }
}

