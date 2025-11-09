using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for error analytics including root cause analysis, correlations, MTTR, and business impact
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/analytics/errors")]
public class ErrorAnalyticsController : ControllerBase
{
    private readonly IErrorAnalyticsService _analyticsService;
    private readonly ILogger<ErrorAnalyticsController> _logger;

    public ErrorAnalyticsController(
        IErrorAnalyticsService analyticsService,
        ILogger<ErrorAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves root cause analysis for errors, categorizing by error type
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Root cause analysis showing error categories and suggested remediations</returns>
    [HttpGet("root-causes")]
    public async Task<IActionResult> GetRootCauseAnalysis(
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

            var rootCauses = await _analyticsService.GetRootCauseAnalysisAsync(start, end);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                root_causes = rootCauses,
                total_categories = rootCauses.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root cause analysis");
            return StatusCode(500, new { error = "Failed to retrieve root cause analysis" });
        }
    }

    /// <summary>
    /// Retrieves error correlations showing jobs that tend to fail together
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Error correlations with correlation strength and potential root causes</returns>
    [HttpGet("correlations")]
    public async Task<IActionResult> GetErrorCorrelations(
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

            var correlations = await _analyticsService.GetErrorCorrelationsAsync(start, end);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                correlations = correlations,
                count = correlations.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error correlations");
            return StatusCode(500, new { error = "Failed to retrieve error correlations" });
        }
    }

    /// <summary>
    /// Retrieves Mean Time To Repair (MTTR) metrics for failed jobs
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>MTTR metrics showing average repair times and trends</returns>
    [HttpGet("mttr")]
    public async Task<IActionResult> GetMttrMetrics(
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

            var metrics = await _analyticsService.GetMttrMetricsAsync(start, end);

            return Ok(new
            {
                period_start = start,
                period_end = end,
                metrics = metrics,
                count = metrics.Count,
                overall_avg_mttr = metrics.Any() ? Math.Round(metrics.Average(m => m.AvgMttr), 1) : 0,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MTTR metrics");
            return StatusCode(500, new { error = "Failed to retrieve MTTR metrics" });
        }
    }

    /// <summary>
    /// Retrieves business impact assessment for errors
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Business impact assessments with criticality scores and cost estimates</returns>
    [HttpGet("business-impact")]
    public async Task<IActionResult> GetBusinessImpact(
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

            var impacts = await _analyticsService.GetBusinessImpactAsync(start, end);

            var totalCost = impacts.Sum(i => i.EstimatedCost);
            var criticalIssues = impacts.Count(i => i.ImpactLevel == "Critical");
            var highIssues = impacts.Count(i => i.ImpactLevel == "High");

            return Ok(new
            {
                period_start = start,
                period_end = end,
                impacts = impacts,
                count = impacts.Count,
                summary = new
                {
                    total_estimated_cost = totalCost,
                    critical_issues = criticalIssues,
                    high_issues = highIssues,
                    total_downtime = impacts.Sum(i => i.TotalDowntime)
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business impact");
            return StatusCode(500, new { error = "Failed to retrieve business impact" });
        }
    }

    /// <summary>
    /// Retrieves comprehensive error analytics summary
    /// </summary>
    /// <param name="startDate">Start date for analysis period (ISO 8601 format)</param>
    /// <param name="endDate">End date for analysis period (ISO 8601 format)</param>
    /// <returns>Summary of error analytics including trends and key metrics</returns>
    [HttpGet("summary")]
    public async Task<IActionResult> GetErrorSummary(
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

            var summary = await _analyticsService.GetErrorSummaryAsync(start, end);

            return Ok(new
            {
                summary = summary,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error summary");
            return StatusCode(500, new { error = "Failed to retrieve error summary" });
        }
    }
}
