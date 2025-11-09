using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for system load analytics and capacity planning
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/analytics/load")]
public class SystemLoadController : ControllerBase
{
    private readonly ISystemLoadAnalyticsService _analyticsService;
    private readonly ILogger<SystemLoadController> _logger;

    public SystemLoadController(
        ISystemLoadAnalyticsService analyticsService,
        ILogger<SystemLoadController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves load heatmap data for batch job executions over time
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <param name="granularity">Time bucket granularity: hourly, daily, weekly (default: hourly)</param>
    /// <returns>Heatmap data showing job count, error count, and resource usage over time</returns>
    [HttpGet("heatmap")]
    public async Task<IActionResult> GetLoadHeatmap(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string granularity = "hourly")
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            if (!new[] { "hourly", "daily", "weekly" }.Contains(granularity.ToLower()))
            {
                return BadRequest(new { error = "Granularity must be one of: hourly, daily, weekly" });
            }

            var heatmapData = await _analyticsService.GetLoadHeatmapAsync(start, end, granularity.ToLower());

            return Ok(heatmapData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting load heatmap data");
            return StatusCode(500, new { error = "Failed to retrieve load heatmap data" });
        }
    }

    /// <summary>
    /// Retrieves AOS server load distribution showing how batch jobs are distributed across servers
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Server load distribution with health status for each AOS server</returns>
    [HttpGet("aos-distribution")]
    public async Task<IActionResult> GetAosServerDistribution(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            var serverDistribution = await _analyticsService.GetAosServerDistributionAsync(start, end);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                servers = serverDistribution,
                total_servers = serverDistribution.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AOS server distribution");
            return StatusCode(500, new { error = "Failed to retrieve AOS server distribution" });
        }
    }

    /// <summary>
    /// Retrieves parallel execution metrics showing concurrent and queued jobs over time
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Parallel execution data with concurrency and queue metrics</returns>
    [HttpGet("parallel-execution")]
    public async Task<IActionResult> GetParallelExecutionMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            var parallelMetrics = await _analyticsService.GetParallelExecutionMetricsAsync(start, end);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                metrics = parallelMetrics,
                count = parallelMetrics.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parallel execution metrics");
            return StatusCode(500, new { error = "Failed to retrieve parallel execution metrics" });
        }
    }

    /// <summary>
    /// Retrieves resource trend data for capacity planning (CPU, memory, I/O, TempDB)
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Resource trend data over time for capacity planning</returns>
    [HttpGet("resource-trends")]
    public async Task<IActionResult> GetResourceTrends(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            var resourceTrends = await _analyticsService.GetResourceTrendsAsync(start, end);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                trends = resourceTrends,
                count = resourceTrends.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource trends");
            return StatusCode(500, new { error = "Failed to retrieve resource trends" });
        }
    }

    /// <summary>
    /// Retrieves comprehensive system load summary with capacity recommendations
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>System load summary with peak load times and capacity recommendations</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSystemLoadSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            var summary = await _analyticsService.GetSystemLoadSummaryAsync(start, end);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system load summary");
            return StatusCode(500, new { error = "Failed to retrieve system load summary" });
        }
    }
}
