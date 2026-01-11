using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lexus2_0.Core.Logging;

namespace Lexus2_0.Core.OCR
{
    /// <summary>
    /// EasyOCR service integration (alternative to Tesseract)
    /// Connects to Python EasyOCR server (like existing implementation)
    /// </summary>
    public class EasyOCRService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;

        public EasyOCRService(ILogger logger, string serverUrl = "http://localhost:5000")
        {
            _logger = logger;
            _serverUrl = serverUrl;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Extract text from base64 image using EasyOCR server
        /// </summary>
        public async Task<string> ExtractTextAsync(string base64Image)
        {
            try
            {
                _logger.Debug("Sending captcha image to EasyOCR server...");

                var requestBody = new
                {
                    image = base64Image
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_serverUrl}/extract-text", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EasyOCRResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null && !string.IsNullOrEmpty(result.ExtractedText))
                {
                    _logger.Info($"OCR extracted text: {result.ExtractedText}");
                    return CleanCaptchaText(result.ExtractedText);
                }

                _logger.Warning("No text extracted from image");
                return string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"EasyOCR server connection error: {ex.Message}", ex);
                throw new Exception("EasyOCR server is not running. Please start the Python server.", ex);
            }
            catch (Exception ex)
            {
                _logger.Error("Error extracting text with EasyOCR", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if EasyOCR server is available
        /// </summary>
        public async Task<bool> IsServerAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serverUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string CleanCaptchaText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove spaces and special characters, keep only alphanumeric
            var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"[^A-Za-z0-9]", "");
            return cleaned.ToUpper();
        }
    }

    public class EasyOCRResponse
    {
        public string ExtractedText { get; set; } = string.Empty;
    }
}

