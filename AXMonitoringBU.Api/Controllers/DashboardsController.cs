using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/dashboards")]
public class DashboardsController : ControllerBase
{
    private readonly IBusinessKpiService _businessKpiService;
    private readonly IKpiDataService _kpiDataService;
    private readonly ILogger<DashboardsController> _logger;

    public DashboardsController(
        IBusinessKpiService businessKpiService,
        IKpiDataService kpiDataService,
        ILogger<DashboardsController> logger)
    {
        _businessKpiService = businessKpiService;
        _kpiDataService = kpiDataService;
        _logger = logger;
    }

    /// <summary>
    /// Get executive dashboard data
    /// </summary>
    [HttpGet("executive")]
    public async Task<IActionResult> GetExecutiveDashboard()
    {
        try
        {
            var kpiData = await _kpiDataService.GetKpiDataAsync();
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();
            
            var technicalMetrics = new Dictionary<string, object>();
            if (kpiData != null)
            {
                foreach (var kvp in kpiData)
                {
                    technicalMetrics[kvp.Key] = kvp.Value;
                }
            }
            if (sqlHealth != null)
            {
                foreach (var kvp in sqlHealth)
                {
                    technicalMetrics[kvp.Key] = kvp.Value;
                }
            }
            
            var businessKpis = await _businessKpiService.CalculateBusinessKpisAsync(technicalMetrics);
            var impactReport = await _businessKpiService.GenerateBusinessImpactReportAsync(technicalMetrics);

            return Ok(new
            {
                kpis = businessKpis,
                impact_report = impactReport,
                technical_metrics = technicalMetrics,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting executive dashboard");
            return StatusCode(500, new { error = "Failed to retrieve executive dashboard" });
        }
    }

    /// <summary>
    /// Get dashboard templates
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetDashboardTemplates()
    {
        var templates = new[]
        {
            new
            {
                id = "executive",
                name = "Executive Dashboard",
                description = "High-level overview for management",
                widgets = new[] { "kpi_summary", "health_score", "critical_alerts", "trend_chart" }
            },
            new
            {
                id = "technical",
                name = "Technical Dashboard",
                description = "Detailed technical metrics",
                widgets = new[] { "performance_metrics", "batch_jobs", "sessions", "sql_health" }
            },
            new
            {
                id = "operations",
                name = "Operations Dashboard",
                description = "Day-to-day operations monitoring",
                widgets = new[] { "alerts", "blocking_chains", "batch_backlog", "active_sessions" }
            }
        };

        return Ok(new
        {
            templates = templates,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get widget data for a specific widget type
    /// </summary>
    [HttpGet("widgets/{widgetType}")]
    public async Task<IActionResult> GetWidgetData(string widgetType)
    {
        try
        {
            var kpiData = await _kpiDataService.GetKpiDataAsync();
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();
            
            object widgetData = widgetType switch
            {
                "kpi_summary" => new
                {
                    type = "kpi_summary",
                    data = new
                    {
                        kpis = kpiData,
                        sql_health = sqlHealth
                    }
                },
                "health_score" => new
                {
                    type = "health_score",
                    data = new
                    {
                        score = 95.5,
                        status = "Good",
                        trend = "Stable"
                    }
                },
                _ => new { type = widgetType, data = new { } }
            };

            return Ok(widgetData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget data for {WidgetType}", widgetType);
            return StatusCode(500, new { error = $"Failed to retrieve widget data for {widgetType}" });
        }
    }
}

