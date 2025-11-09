using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for managing alerts and notifications
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/alerts")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly IExportService _exportService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(
        IAlertService alertService, 
        IExportService exportService,
        ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all alerts, optionally filtered by status
    /// </summary>
    /// <param name="status">Optional status filter (Active, Resolved, Acknowledged)</param>
    /// <returns>List of alerts</returns>
    [HttpGet]
    public async Task<IActionResult> GetAlerts([FromQuery] string? status = null)
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync(status);

            return Ok(new
            {
                alerts = alerts.Select(a => new
                {
                    a.Id,
                    a.AlertId,
                    a.Type,
                    a.Severity,
                    a.Message,
                    a.Status,
                    a.Timestamp,
                    a.ResolvedAt,
                    a.CreatedBy
                }),
                count = alerts.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, new { error = "Failed to retrieve alerts" });
        }
    }

    /// <summary>
    /// Creates a new alert
    /// </summary>
    /// <param name="dto">Alert creation data</param>
    /// <returns>Created alert</returns>
    [HttpPost]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto dto)
    {
        try
        {
            var alert = await _alertService.CreateAlertAsync(
                dto.Type,
                dto.Severity,
                dto.Message,
                User.Identity?.Name);

            return CreatedAtAction(nameof(GetAlert), new { id = alert.Id }, new
            {
                alert = new
                {
                    alert.Id,
                    alert.AlertId,
                    alert.Type,
                    alert.Severity,
                    alert.Message,
                    alert.Status,
                    alert.Timestamp
                },
                message = "Alert created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert");
            return StatusCode(500, new { error = "Failed to create alert" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAlert(int id)
    {
        var alert = await _alertService.GetAlertByIdAsync(id);
        if (alert == null)
        {
            return NotFound();
        }

        return Ok(alert);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAlert(int id, [FromBody] UpdateAlertDto dto)
    {
        var success = await _alertService.UpdateAlertStatusAsync(id, dto.Status ?? "Active");
        if (!success)
        {
            return NotFound();
        }

        return Ok(new { message = $"Alert {id} updated successfully" });
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int id)
    {
        try
        {
            var success = await _alertService.AcknowledgeAlertAsync(id, User.Identity?.Name ?? "Unknown");
            if (!success)
            {
                return NotFound();
            }
            return Ok(new { message = $"Alert {id} acknowledged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            return StatusCode(500, new { error = "Failed to acknowledge alert" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAlert(int id)
    {
        var success = await _alertService.DeleteAlertAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return Ok(new { message = $"Alert {id} deleted successfully" });
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportAlertsToCsv([FromQuery] string? status = null)
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync(status);
            var csvBytes = await _exportService.ExportAlertsToCsvAsync(alerts);
            var filename = $"alerts_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting alerts to CSV");
            return StatusCode(500, new { error = "Failed to export alerts" });
        }
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportAlertsToExcel([FromQuery] string? status = null)
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync(status);
            var excelBytes = await _exportService.ExportAlertsToExcelAsync(alerts);
            var filename = $"alerts_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting alerts to Excel");
            return StatusCode(500, new { error = "Failed to export alerts" });
        }
    }
}

public class CreateAlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UpdateAlertDto
{
    public string? Status { get; set; }
}

