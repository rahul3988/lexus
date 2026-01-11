using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lexus2_0.Core.Models;

namespace Lexus2_0.Desktop.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ApiClientService(string apiBaseUrl = "http://localhost:5000")
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiBaseUrl = apiBaseUrl;
        }

        public async Task<string?> FetchTokenAsync(TokenConfig tokenConfig)
        {
            try
            {
                var json = JsonSerializer.Serialize(tokenConfig);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/token/fetch", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TokenApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Token;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/token/validate?token={Uri.EscapeDataString(token)}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TokenValidationApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result?.Valid ?? false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private class TokenApiResponse
        {
            public bool Success { get; set; }
            public string? Token { get; set; }
            public string? Message { get; set; }
        }

        private class TokenValidationApiResponse
        {
            public bool Success { get; set; }
            public bool Valid { get; set; }
            public string? Message { get; set; }
        }
    }
}

