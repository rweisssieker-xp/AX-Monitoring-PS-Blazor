using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;
using System.Text.Json;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IPdfReportService _pdfService;
    private readonly IKpiDataService _kpiService;
    private readonly IBatchJobService _batchJobService;
    private readonly ISessionService _sessionService;
    private readonly IAlertService _alertService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IPdfReportService pdfService,
        IKpiDataService kpiService,
        IBatchJobService batchJobService,
        ISessionService sessionService,
        IAlertService alertService,
        ILogger<ReportsController> logger)
    {
        _pdfService = pdfService;
        _kpiService = kpiService;
        _batchJobService = batchJobService;
        _sessionService = sessionService;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Generate executive summary PDF report
    /// </summary>
    [HttpPost("executive")]
    public async Task<IActionResult> GenerateExecutiveReport([FromBody] GenerateReportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var reportData = await PrepareReportDataAsync(request.Status, cancellationToken);
            
            var pdfBytes = await _pdfService.GenerateExecutiveSummaryAsync(
                reportData, 
                request.Period ?? "monthly", 
                cancellationToken);

            var filename = $"executive_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating executive report");
            return StatusCode(500, new { message = "Failed to generate executive report", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate detailed PDF report
    /// </summary>
    [HttpPost("detailed")]
    public async Task<IActionResult> GenerateDetailedReport([FromBody] GenerateReportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var reportData = await PrepareReportDataAsync(request.Status, cancellationToken);
            
            var pdfBytes = await _pdfService.GenerateDetailedReportAsync(
                reportData, 
                request.Period ?? "monthly", 
                cancellationToken);

            var filename = $"detailed_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating detailed report");
            return StatusCode(500, new { message = "Failed to generate detailed report", error = ex.Message });
        }
    }

    private async Task<ReportData> PrepareReportDataAsync(string? alertStatus, CancellationToken cancellationToken)
    {
        var kpiData = await _kpiService.GetKpiDataAsync();
        var sqlHealth = await _kpiService.GetSqlHealthAsync();

        var kpiDataObj = kpiData != null ? new KpiData
        {
            batch_backlog = kpiData.TryGetValue("batch_backlog", out var backlog) ? Convert.ToInt32(backlog) : 0,
            error_rate = kpiData.TryGetValue("error_rate", out var errorRate) ? Convert.ToDouble(errorRate) : 0,
            active_sessions = kpiData.TryGetValue("active_sessions", out var sessions) ? Convert.ToInt32(sessions) : 0,
            blocking_chains = kpiData.TryGetValue("blocking_chains", out var blocking) ? Convert.ToInt32(blocking) : 0
        } : null;

        var sqlHealthObj = sqlHealth != null ? new SqlHealthData
        {
            cpu_usage = sqlHealth.TryGetValue("cpu_usage", out var cpu) ? Convert.ToDouble(cpu) : 0,
            memory_usage = sqlHealth.TryGetValue("memory_usage", out var memory) ? Convert.ToDouble(memory) : 0,
            io_wait = sqlHealth.TryGetValue("io_wait", out var io) ? Convert.ToDouble(io) : 0,
            tempdb_usage = sqlHealth.TryGetValue("tempdb_usage", out var tempdb) ? Convert.ToDouble(tempdb) : 0,
            active_connections = sqlHealth.TryGetValue("active_connections", out var connections) ? Convert.ToInt32(connections) : 0,
            longest_running_query = sqlHealth.TryGetValue("longest_running_query", out var query) ? Convert.ToInt32(query) : 0
        } : null;

        var batchJobs = await _batchJobService.GetBatchJobsAsync();
        var batchJobDtos = batchJobs?.Select(b => new BatchJobDto
        {
            BatchJobId = b.BatchJobId,
            Name = b.Name,
            Status = b.Status,
            Progress = b.Progress
        }).ToList();

        var alerts = await _alertService.GetAlertsAsync(alertStatus);
        var alertDtos = alerts?.Select(a => new AlertDto
        {
            Type = a.Type,
            Severity = a.Severity,
            Message = a.Message,
            Status = a.Status,
            Timestamp = a.Timestamp
        }).ToList();

        var recommendations = new List<string>
        {
            "System performance is within normal parameters",
            "Continue monitoring batch job execution times",
            "Review blocking chains regularly for optimization opportunities"
        };

        return new ReportData
        {
            Environment = "PROD",
            Kpis = kpiDataObj,
            SqlHealth = sqlHealthObj,
            BatchJobs = batchJobDtos,
            Alerts = alertDtos,
            Recommendations = recommendations
        };
    }
}

public class GenerateReportRequest
{
    public string? Period { get; set; } = "monthly"; // daily, weekly, monthly
    public string? Status { get; set; } // Alert status filter
}

