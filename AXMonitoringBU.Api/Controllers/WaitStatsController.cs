using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/wait-stats")]
public class WaitStatsController : ControllerBase
{
    private readonly IWaitStatsService _waitStatsService;
    private readonly ILogger<WaitStatsController> _logger;

    public WaitStatsController(
        IWaitStatsService waitStatsService,
        ILogger<WaitStatsController> logger)
    {
        _waitStatsService = waitStatsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetWaitStats([FromQuery] int topN = 20)
    {
        try
        {
            var waitStats = await _waitStatsService.GetWaitStatsAsync(topN);
            return Ok(new
            {
                wait_stats = waitStats,
                count = waitStats.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wait stats");
            return StatusCode(500, new { error = "Failed to retrieve wait stats" });
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetWaitStatsSummary()
    {
        try
        {
            var summary = await _waitStatsService.GetWaitStatsSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wait stats summary");
            return StatusCode(500, new { error = "Failed to retrieve wait stats summary" });
        }
    }

    [HttpGet("top-queries")]
    public async Task<IActionResult> GetTopQueries(
        [FromQuery] int topN = 20,
        [FromQuery] double? minDurationMs = null)
    {
        try
        {
            var minDuration = minDurationMs.HasValue ? TimeSpan.FromMilliseconds(minDurationMs.Value) : TimeSpan.Zero;
            var topQueries = await _waitStatsService.GetTopQueriesAsync(topN, minDuration);
            return Ok(new
            {
                top_queries = topQueries,
                count = topQueries.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top queries");
            return StatusCode(500, new { error = "Failed to retrieve top queries" });
        }
    }
}

