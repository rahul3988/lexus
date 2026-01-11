using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Lexus2_0.Core.Logging;
using Lexus2_0.Core.Models;

namespace Lexus2_0.Core.Token
{
    /// <summary>
    /// Token manager for TeslaX-style token-based booking
    /// Handles token generation, fetching, and validation
    /// </summary>
    public class TokenManager
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private string? _cachedToken;
        private DateTime _tokenExpiry;

        public TokenManager(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Fetch token from API (TeslaX APK token generation)
        /// </summary>
        public async Task<string?> FetchTokenAsync(TokenConfig tokenConfig)
        {
            if (tokenConfig == null || !tokenConfig.UseToken)
            {
                _logger.Debug("Token not configured or not enabled");
                return null;
            }

            // If token is already provided, use it
            if (!string.IsNullOrEmpty(tokenConfig.Token))
            {
                _logger.Info("Using provided token");
                _cachedToken = tokenConfig.Token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenConfig.TokenRefreshInterval);
                return _cachedToken;
            }

            // If TokenApiUrl is provided, fetch token from API
            if (string.IsNullOrEmpty(tokenConfig.TokenApiUrl))
            {
                _logger.Warning("Token enabled but no TokenApiUrl or Token provided");
                return null;
            }

            try
            {
                _logger.Info($"Fetching token from API: {tokenConfig.TokenApiUrl}");

                var request = new HttpRequestMessage(HttpMethod.Get, tokenConfig.TokenApiUrl);

                // Add authorization header if provided
                if (!string.IsNullOrEmpty(tokenConfig.TokenAuthHeader) && 
                    !string.IsNullOrEmpty(tokenConfig.TokenAuthValue))
                {
                    request.Headers.Add(tokenConfig.TokenAuthHeader, tokenConfig.TokenAuthValue);
                }

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Try to parse as JSON first (common format: {"token": "..."})
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(content);
                        if (jsonDoc.RootElement.TryGetProperty("token", out var tokenElement))
                        {
                            _cachedToken = tokenElement.GetString();
                        }
                        else if (jsonDoc.RootElement.TryGetProperty("Token", out var tokenElement2))
                        {
                            _cachedToken = tokenElement2.GetString();
                        }
                        else if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement))
                        {
                            if (dataElement.TryGetProperty("token", out var nestedToken))
                            {
                                _cachedToken = nestedToken.GetString();
                            }
                        }
                    }
                    catch
                    {
                        // If not JSON, treat as plain text token
                        _cachedToken = content.Trim();
                    }

                    if (!string.IsNullOrEmpty(_cachedToken))
                    {
                        _logger.Info("Token fetched successfully");
                        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenConfig.TokenRefreshInterval);
                        return _cachedToken;
                    }
                    else
                    {
                        _logger.Warning("Token API returned empty response");
                        return null;
                    }
                }
                else
                {
                    _logger.Error($"Token API returned error: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error fetching token: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Get cached token if still valid
        /// </summary>
        public string? GetCachedToken()
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }
            return null;
        }

        /// <summary>
        /// Clear cached token
        /// </summary>
        public void ClearToken()
        {
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

