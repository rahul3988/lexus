using Microsoft.AspNetCore.Mvc;
using Lexus2_0.Core.Models;
using Lexus2_0.Core.Token;
using CoreLogger = Lexus2_0.Core.Logging.ILogger;

namespace Lexus2_0.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly CoreLogger _logger;
        private readonly TokenManager _tokenManager;

        public TokenController(CoreLogger logger)
        {
            _logger = logger;
            _tokenManager = new TokenManager(logger);
        }

        /// <summary>
        /// Fetch token from API (TeslaX-style token generation)
        /// POST /api/token/fetch
        /// </summary>
        [HttpPost("fetch")]
        public async Task<IActionResult> FetchToken([FromBody] TokenConfig tokenConfig)
        {
            if (tokenConfig == null)
            {
                return BadRequest(new { success = false, message = "Token configuration required" });
            }

            if (!tokenConfig.IsValid())
            {
                return BadRequest(new { success = false, message = "Invalid token configuration" });
            }

            try
            {
                var token = await _tokenManager.FetchTokenAsync(tokenConfig);
                
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "Failed to fetch token" });
                }

                return Ok(new 
                { 
                    success = true, 
                    token = token,
                    message = "Token fetched successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in token fetch: {ex.Message}", ex);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Validate token
        /// GET /api/token/validate?token=xxx
        /// </summary>
        [HttpGet("validate")]
        public IActionResult ValidateToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { success = false, message = "Token required" });
            }

            // Basic validation - token should not be empty
            var isValid = !string.IsNullOrWhiteSpace(token) && token.Length > 10;
            
            return Ok(new 
            { 
                success = isValid, 
                valid = isValid,
                message = isValid ? "Token is valid" : "Token is invalid"
            });
        }
    }
}

