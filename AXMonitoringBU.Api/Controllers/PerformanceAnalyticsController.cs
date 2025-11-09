using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for performance analytics including job duration trends, baseline comparisons, and predictive warnings
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/analytics/performance")]
public class PerformanceAnalyticsController : ControllerBase
{
    private readonly IPerformanceAnalyticsService _analyticsService;
    private readonly ILogger<PerformanceAnalyticsController> _logger;

    public PerformanceAnalyticsController(
        IPerformanceAnalyticsService analyticsService,
        ILogger<PerformanceAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves job duration trends over time to identify performance patterns
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <param name="jobCaption">Optional job caption filter to analyze specific job (partial match)</param>
    /// <returns>Job duration trends with average, min, max, and standard deviation</returns>
    [HttpGet("duration-trends")]
    public async Task<IActionResult> GetJobDurationTrends(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? jobCaption = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            var trends = await _analyticsService.GetJobDurationTrendsAsync(start, end, jobCaption);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                job_filter = jobCaption ?? "all",
                trends = trends,
                count = trends.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job duration trends");
            return StatusCode(500, new { error = "Failed to retrieve job duration trends" });
        }
    }

    /// <summary>
    /// Compares current job performance against historical baseline (30 days prior)
    /// </summary>
    /// <param name="startDate">Start date for current period (ISO 8601 format)</param>
    /// <param name="endDate">End date for current period (ISO 8601 format)</param>
    /// <returns>Baseline comparison showing jobs that are slower or faster than historical baseline</returns>
    [HttpGet("baseline-comparison")]
    public async Task<IActionResult> GetBaselineComparison(
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

            var comparison = await _analyticsService.GetBaselineComparisonAsync(start, end);

            return Ok(new
            {
                current_period = new { start, end },
                baseline_period = new { start = start.AddDays(-30), end = start },
                comparison = comparison,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting baseline comparison");
            return StatusCode(500, new { error = "Failed to retrieve baseline comparison" });
        }
    }

    /// <summary>
    /// Retrieves the slowest job executions to identify performance bottlenecks
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <param name="topN">Number of slowest operations to return (default: 20, max: 100)</param>
    /// <returns>List of slowest job executions with duration and execution details</returns>
    [HttpGet("slowest-operations")]
    public async Task<IActionResult> GetSlowestOperations(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int topN = 20)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            if (end <= start)
            {
                return BadRequest(new { error = "End date must be after start date" });
            }

            if (topN < 1 || topN > 100)
            {
                return BadRequest(new { error = "topN must be between 1 and 100" });
            }

            var slowestOps = await _analyticsService.GetSlowestOperationsAsync(start, end, topN);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                requested_count = topN,
                operations = slowestOps,
                actual_count = slowestOps.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slowest operations");
            return StatusCode(500, new { error = "Failed to retrieve slowest operations" });
        }
    }

    /// <summary>
    /// Generates predictive warnings for jobs with concerning performance trends
    /// </summary>
    /// <returns>List of predictive warnings with severity levels and recommended actions</returns>
    [HttpGet("predictive-warnings")]
    public async Task<IActionResult> GetPredictiveWarnings()
    {
        try
        {
            var warnings = await _analyticsService.GetPredictiveWarningsAsync();

            var highSeverity = warnings.Count(w => w.Severity == "High");
            var mediumSeverity = warnings.Count(w => w.Severity == "Medium");
            var lowSeverity = warnings.Count(w => w.Severity == "Low");

            return Ok(new
            {
                warnings = warnings,
                total_warnings = warnings.Count,
                severity_breakdown = new
                {
                    high = highSeverity,
                    medium = mediumSeverity,
                    low = lowSeverity
                },
                analysis_period = new
                {
                    start = DateTime.UtcNow.AddDays(-7),
                    end = DateTime.UtcNow
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictive warnings");
            return StatusCode(500, new { error = "Failed to retrieve predictive warnings" });
        }
    }
}
