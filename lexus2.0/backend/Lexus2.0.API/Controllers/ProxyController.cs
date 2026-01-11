using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lexus2_0.Core.Proxy;
using CoreLogger = Lexus2_0.Core.Logging.ILogger;

namespace Lexus2_0.API.Controllers
{
    [ApiController]
    [Route("api/proxy")]
    public class ProxyController : ControllerBase
    {
        private readonly CoreLogger _logger;

        public ProxyController(CoreLogger logger)
        {
            _logger = logger;
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestProxy([FromBody] ProxyConfiguration proxyConfig)
        {
            if (proxyConfig == null || !proxyConfig.Enabled)
            {
                return BadRequest(new { success = false, message = "Proxy not enabled" });
            }

            if (!proxyConfig.IsValid())
            {
                return BadRequest(new { success = false, message = "Invalid proxy configuration" });
            }

            try
            {
                _logger.Info($"Testing proxy: {proxyConfig.Host}:{proxyConfig.Port}");

                // Create HTTP client with proxy
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy($"{proxyConfig.Type.ToString().ToLower()}://{proxyConfig.Host}:{proxyConfig.Port}")
                };

                if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
                {
                    handler.Proxy.Credentials = new NetworkCredential(proxyConfig.Username, proxyConfig.Password);
                }

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                // Test connection by checking IP
                var response = await client.GetAsync("https://api.ipify.org?format=json");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var ipData = System.Text.Json.JsonSerializer.Deserialize<IpResponse>(content);
                    
                    _logger.Info($"Proxy test successful. IP: {ipData?.Ip}");
                    return Ok(new { success = true, message = "Proxy connection successful", ip = ipData?.Ip });
                }
                else
                {
                    _logger.Warning($"Proxy test failed. Status: {response.StatusCode}");
                    return BadRequest(new { success = false, message = $"Proxy test failed: {response.StatusCode}" });
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"Proxy connection error: {ex.Message}", ex);
                return BadRequest(new { success = false, message = $"Connection failed: {ex.Message}" });
            }
            catch (TaskCanceledException)
            {
                _logger.Error("Proxy test timeout");
                return BadRequest(new { success = false, message = "Connection timeout" });
            }
            catch (Exception ex)
            {
                _logger.Error($"Proxy test error: {ex.Message}", ex);
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet("check")]
        public IActionResult CheckProxy()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = client.GetAsync("https://api.ipify.org?format=json").Result;
                
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    var ipData = System.Text.Json.JsonSerializer.Deserialize<IpResponse>(content);
                    return Ok(new { success = true, ip = ipData?.Ip, message = "Current IP address" });
                }
                
                return BadRequest(new { success = false, message = "Failed to get IP" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class IpResponse
    {
        public string? Ip { get; set; }
    }
}

