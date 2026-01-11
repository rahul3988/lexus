using Microsoft.AspNetCore.Mvc;
using Lexus2_0.Core.Models;
using Lexus2_0.Core.Config;
using Lexus2_0.Automation;
using Lexus2_0.Core.StateMachine;

namespace Lexus2_0.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly AutomationEngine _automationEngine;
        private readonly ConfigManager _configManager;

        public BookingController(AutomationEngine automationEngine, ConfigManager configManager)
        {
            _automationEngine = automationEngine;
            _configManager = configManager;
        }

        [HttpPost("start")]
        public IActionResult Start([FromBody] BookingConfig config)
        {
            if (!_configManager.ValidateConfig(config))
            {
                return BadRequest(new { success = false, message = "Invalid configuration" });
            }

            // Validate proxy if enabled
            if (config.ProxyConfig != null && config.ProxyConfig.Enabled && !config.ProxyConfig.IsValid())
            {
                return BadRequest(new { success = false, message = "Invalid proxy configuration" });
            }

            try
            {
                _automationEngine.StartAsync(config).Wait();
                return Ok(new { success = true, message = "Booking started" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            _automationEngine.Stop();
            return Ok(new { success = true, message = "Booking stopped" });
        }

        [HttpPost("pause")]
        public IActionResult Pause()
        {
            _automationEngine.Pause();
            return Ok(new { success = true, message = "Booking paused" });
        }

        [HttpPost("resume")]
        public IActionResult Resume()
        {
            _automationEngine.Resume();
            return Ok(new { success = true, message = "Booking resumed" });
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                isRunning = _automationEngine.IsRunning,
                currentState = _automationEngine.CurrentState.ToString()
            });
        }

        [HttpPost("config/save")]
        public IActionResult SaveConfig([FromBody] BookingConfig config, [FromQuery] bool encrypt = false)
        {
            if (!_configManager.ValidateConfig(config))
            {
                return BadRequest(new { success = false, message = "Invalid configuration" });
            }

            _configManager.SaveConfig(config, encrypt);
            return Ok(new { success = true, message = "Configuration saved" });
        }

        [HttpGet("config/load")]
        public IActionResult LoadConfig([FromQuery] bool encrypted = false)
        {
            var config = _configManager.LoadConfig(encrypted);
            if (config == null)
            {
                return NotFound(new { success = false, message = "No configuration found" });
            }

            return Ok(new { success = true, config });
        }
    }
}

