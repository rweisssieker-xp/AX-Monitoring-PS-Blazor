using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] string? status = null)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsAsync(status);

            return Ok(new
            {
                sessions = sessions,
                count = sessions.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, new { error = "Failed to retrieve sessions" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _sessionService.GetSessionByIdAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpPost("{id}/kill")]
    public async Task<IActionResult> KillSession(int id)
    {
        try
        {
            var success = await _sessionService.KillSessionAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new
            {
                message = $"Session {id} termination initiated",
                session_id = id,
                status = "terminating"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing session {SessionId}", id);
            return StatusCode(500, new { error = "Failed to kill session" });
        }
    }
}

