using System.Text.Json.Serialization;

namespace Lexus2_0.Core.Models
{
    /// <summary>
    /// Token configuration for TeslaX-style token-based booking
    /// </summary>
    public class TokenConfig
    {
        [JsonPropertyName("TokenApiUrl")]
        public string? TokenApiUrl { get; set; } // APK/API endpoint URL for token generation

        [JsonPropertyName("Token")]
        public string? Token { get; set; } // Pre-generated token (if available)

        [JsonPropertyName("TokenAuthHeader")]
        public string? TokenAuthHeader { get; set; } // Authorization header name (e.g., "Authorization", "X-Token")

        [JsonPropertyName("TokenAuthValue")]
        public string? TokenAuthValue { get; set; } // Authorization value/API key if needed

        [JsonPropertyName("UseToken")]
        public bool UseToken { get; set; } = false; // Whether to use token-based booking

        [JsonPropertyName("TokenRefreshInterval")]
        public int TokenRefreshInterval { get; set; } = 3600; // Token refresh interval in seconds

        /// <summary>
        /// Validate token configuration
        /// </summary>
        public bool IsValid()
        {
            if (!UseToken)
                return true; // Not using token is valid

            // If using token, either TokenApiUrl or Token must be provided
            return !string.IsNullOrEmpty(TokenApiUrl) || !string.IsNullOrEmpty(Token);
        }
    }
}

