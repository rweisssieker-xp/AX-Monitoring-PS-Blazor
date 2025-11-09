using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for managing alert correlations and incidents
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/alerts/correlation")]
public class AlertCorrelationController : ControllerBase
{
    private readonly IAlertCorrelationService _correlationService;
    private readonly ILogger<AlertCorrelationController> _logger;

    public AlertCorrelationController(
        IAlertCorrelationService correlationService,
        ILogger<AlertCorrelationController> logger)
    {
        _correlationService = correlationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all correlations (incidents)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCorrelations([FromQuery] string? status = null)
    {
        try
        {
            var correlations = await _correlationService.GetCorrelationsAsync(status);
            return Ok(new { correlations, count = correlations.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correlations");
            return StatusCode(500, new { error = "Failed to retrieve correlations" });
        }
    }

    /// <summary>
    /// Get correlation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCorrelation(int id)
    {
        try
        {
            var correlation = await _correlationService.GetCorrelationByIdAsync(id);
            if (correlation == null)
            {
                return NotFound();
            }
            return Ok(correlation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correlation {CorrelationId}", id);
            return StatusCode(500, new { error = "Failed to retrieve correlation" });
        }
    }

    /// <summary>
    /// Manually trigger correlation check
    /// </summary>
    [HttpPost("correlate")]
    public async Task<IActionResult> CorrelateAlerts()
    {
        try
        {
            var correlation = await _correlationService.CorrelateAlertsAsync();
            if (correlation == null)
            {
                return Ok(new { message = "No new correlations found" });
            }
            return Ok(new { correlation, message = "Correlation created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error correlating alerts");
            return StatusCode(500, new { error = "Failed to correlate alerts" });
        }
    }

    /// <summary>
    /// Resolve a correlation (incident)
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveCorrelation(int id)
    {
        try
        {
            var success = await _correlationService.ResolveCorrelationAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return Ok(new { message = "Correlation resolved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving correlation {CorrelationId}", id);
            return StatusCode(500, new { error = "Failed to resolve correlation" });
        }
    }

    /// <summary>
    /// Get alerts for a specific correlation
    /// </summary>
    [HttpGet("{id}/alerts")]
    public async Task<IActionResult> GetAlertsForCorrelation(int id)
    {
        try
        {
            var alerts = await _correlationService.GetAlertsForCorrelationAsync(id);
            return Ok(new { alerts, count = alerts.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts for correlation {CorrelationId}", id);
            return StatusCode(500, new { error = "Failed to retrieve alerts" });
        }
    }
}

