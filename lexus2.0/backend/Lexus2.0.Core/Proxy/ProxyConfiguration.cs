using System;
using System.Text.Json.Serialization;

namespace Lexus2_0.Core.Proxy
{
    /// <summary>
    /// Proxy configuration (inspired by NeXuS implementation)
    /// </summary>
    public class ProxyConfiguration
    {
        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = false;
        
        [JsonPropertyName("Host")]
        public string Host { get; set; } = string.Empty;
        
        [JsonPropertyName("Port")]
        public int Port { get; set; } = 8080;
        
        [JsonPropertyName("Username")]
        public string? Username { get; set; }
        
        [JsonPropertyName("Password")]
        public string? Password { get; set; }
        
        [JsonPropertyName("Type")]
        public ProxyType Type { get; set; } = ProxyType.Http;

        /// <summary>
        /// Get proxy server string for Playwright
        /// </summary>
        public string GetProxyServer()
        {
            if (!Enabled || string.IsNullOrEmpty(Host))
                return string.Empty;

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                return $"{Type.ToString().ToLower()}://{Username}:{Password}@{Host}:{Port}";
            }

            return $"{Type.ToString().ToLower()}://{Host}:{Port}";
        }

        /// <summary>
        /// Validate proxy configuration
        /// </summary>
        public bool IsValid()
        {
            if (!Enabled)
                return true; // No proxy is valid

            return !string.IsNullOrEmpty(Host) && Port > 0 && Port <= 65535;
        }
    }

    public enum ProxyType
    {
        Http,
        Https,
        Socks4,
        Socks5
    }
}

