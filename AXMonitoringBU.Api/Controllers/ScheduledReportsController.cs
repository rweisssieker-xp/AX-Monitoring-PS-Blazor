using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/scheduled-reports")]
public class ScheduledReportsController : ControllerBase
{
    private readonly IScheduledReportService _scheduledReportService;
    private readonly ILogger<ScheduledReportsController> _logger;

    public ScheduledReportsController(
        IScheduledReportService scheduledReportService,
        ILogger<ScheduledReportsController> logger)
    {
        _scheduledReportService = scheduledReportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetScheduledReports()
    {
        try
        {
            var reports = await _scheduledReportService.GetScheduledReportsAsync();
            return Ok(new
            {
                scheduled_reports = reports,
                count = reports.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduled reports");
            return StatusCode(500, new { error = "Failed to retrieve scheduled reports" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateScheduledReport([FromBody] CreateScheduledReportRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.ReportType))
            {
                return BadRequest(new { error = "Name and ReportType are required" });
            }

            var report = new ScheduledReport
            {
                Name = request.Name,
                ReportType = request.ReportType,
                Schedule = request.Schedule ?? "daily",
                CronExpression = request.CronExpression,
                Recipients = request.Recipients ?? "",
                Enabled = request.Enabled ?? true
            };

            var created = await _scheduledReportService.CreateScheduledReportAsync(report);
            return Ok(new
            {
                scheduled_report = created,
                message = "Scheduled report created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scheduled report");
            return StatusCode(500, new { error = "Failed to create scheduled report" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScheduledReport(int id, [FromBody] UpdateScheduledReportRequest request)
    {
        try
        {
            var report = new ScheduledReport
            {
                Name = request.Name ?? "",
                ReportType = request.ReportType ?? "",
                Schedule = request.Schedule ?? "daily",
                CronExpression = request.CronExpression,
                Recipients = request.Recipients ?? "",
                Enabled = request.Enabled ?? true
            };

            var updated = await _scheduledReportService.UpdateScheduledReportAsync(id, report);
            if (!updated)
            {
                return NotFound(new { error = "Scheduled report not found" });
            }

            return Ok(new { message = "Scheduled report updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled report");
            return StatusCode(500, new { error = "Failed to update scheduled report" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScheduledReport(int id)
    {
        try
        {
            var deleted = await _scheduledReportService.DeleteScheduledReportAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "Scheduled report not found" });
            }

            return Ok(new { message = "Scheduled report deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scheduled report");
            return StatusCode(500, new { error = "Failed to delete scheduled report" });
        }
    }

    [HttpPost("{id}/execute")]
    public async Task<IActionResult> ExecuteScheduledReport(int id)
    {
        try
        {
            var executed = await _scheduledReportService.ExecuteScheduledReportAsync(id);
            if (!executed)
            {
                return NotFound(new { error = "Scheduled report not found or disabled" });
            }

            return Ok(new { message = "Scheduled report executed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled report");
            return StatusCode(500, new { error = "Failed to execute scheduled report" });
        }
    }
}

public class CreateScheduledReportRequest
{
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string? Schedule { get; set; }
    public string? CronExpression { get; set; }
    public string? Recipients { get; set; }
    public bool? Enabled { get; set; }
}

public class UpdateScheduledReportRequest
{
    public string? Name { get; set; }
    public string? ReportType { get; set; }
    public string? Schedule { get; set; }
    public string? CronExpression { get; set; }
    public string? Recipients { get; set; }
    public bool? Enabled { get; set; }
}

