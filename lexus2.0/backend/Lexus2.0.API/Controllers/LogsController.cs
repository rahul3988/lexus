using Microsoft.AspNetCore.Mvc;
using Lexus2_0.Core.Logging;

namespace Lexus2_0.API.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogsController : ControllerBase
    {
        private readonly InMemoryLogger _logger;

        public LogsController(InMemoryLogger logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetLogs([FromQuery] int? limit = 100)
        {
            var logs = _logger.GetLogs(limit);
            return Ok(logs.Select(l => new
            {
                timestamp = l.Timestamp,
                level = l.Level,
                message = l.Message
            }));
        }

        [HttpPost("clear")]
        public IActionResult ClearLogs()
        {
            _logger.ClearLogs();
            return Ok(new { success = true, message = "Logs cleared" });
        }
    }
}

