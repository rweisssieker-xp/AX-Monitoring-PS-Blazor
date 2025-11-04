using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/deadlocks")]
public class DeadlocksController : ControllerBase
{
    private readonly IDeadlockService _deadlockService;
    private readonly ILogger<DeadlocksController> _logger;

    public DeadlocksController(
        IDeadlockService deadlockService,
        ILogger<DeadlocksController> logger)
    {
        _deadlockService = deadlockService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDeadlocks([FromQuery] int count = 100)
    {
        try
        {
            var deadlocks = await _deadlockService.GetRecentDeadlocksAsync(count);

            return Ok(new
            {
                deadlocks = deadlocks.Select(d => new
                {
                    d.Id,
                    d.Timestamp,
                    d.VictimSessionId,
                    process_count = d.Processes.Count,
                    resource_count = d.Resources.Count,
                    processes = d.Processes.Select(p => new
                    {
                        p.ProcessId,
                        p.SessionId,
                        p.DatabaseName,
                        p.IsVictim,
                        sql_text_preview = p.SqlText.Length > 100 ? p.SqlText.Substring(0, 100) + "..." : p.SqlText
                    }),
                    resources = d.Resources.Select(r => new
                    {
                        r.ResourceType,
                        r.ObjectName,
                        r.IndexName
                    })
                }),
                count = deadlocks.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlocks");
            return StatusCode(500, new { error = "Failed to retrieve deadlocks" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeadlockById(string id)
    {
        try
        {
            var deadlock = await _deadlockService.GetDeadlockByIdAsync(id);
            if (deadlock == null)
            {
                return NotFound(new { error = $"Deadlock {id} not found" });
            }

            return Ok(new
            {
                deadlock = new
                {
                    deadlock.Id,
                    deadlock.Timestamp,
                    deadlock.VictimSessionId,
                    deadlock.DeadlockXml,
                    deadlock.Processes,
                    deadlock.Resources
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlock by id {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve deadlock" });
        }
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetDeadlockCount([FromQuery] DateTime? since = null)
    {
        try
        {
            var count = await _deadlockService.GetDeadlockCountAsync(since);

            return Ok(new
            {
                count,
                since = since ?? DateTime.UtcNow.AddDays(-1),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlock count");
            return StatusCode(500, new { error = "Failed to retrieve deadlock count" });
        }
    }
}

