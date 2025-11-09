using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for retrieving monitoring metrics and KPIs
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IKpiDataService _kpiDataService;
    private readonly IBusinessKpiService _businessKpiService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IKpiDataService kpiDataService,
        IBusinessKpiService businessKpiService,
        ILogger<MetricsController> logger)
    {
        _kpiDataService = kpiDataService;
        _businessKpiService = businessKpiService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves historical metrics data for the specified time range
    /// </summary>
    /// <param name="metric">Optional metric name to filter (e.g., cpu_usage, memory_usage, active_sessions)</param>
    /// <param name="timeRange">Time range: 24h, 7d, 30d, or 90d (default: 24h)</param>
    /// <returns>Historical metrics data</returns>
    [HttpGet("history")]
    public IActionResult GetMetricsHistory([FromQuery] string? metric = null, [FromQuery] string timeRange = "24h")
    {
        try
        {
            // Generate mock historical data
            var historicalData = GenerateHistoricalData(metric, timeRange);

            return Ok(new
            {
                metric = metric ?? "all",
                time_range = timeRange,
                data = historicalData,
                count = historicalData.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics history");
            return StatusCode(500, new { error = "Failed to retrieve metrics history" });
        }
    }

    private List<Dictionary<string, object>> GenerateHistoricalData(string? metric, string timeRange)
    {
        var data = new List<Dictionary<string, object>>();
        var random = new Random();
        
        int pointCount = timeRange switch
        {
            "24h" => 24,
            "7d" => 7,
            "30d" => 30,
            "90d" => 90,
            _ => 24
        };

        var baseValue = metric switch
        {
            "cpu_usage" => 50.0,
            "memory_usage" => 60.0,
            "active_sessions" => 100.0,
            "error_rate" => 1.5,
            "blocking_chains" => 2.0,
            _ => 50.0
        };

        for (int i = 0; i < pointCount; i++)
        {
            var timestamp = timeRange switch
            {
                "24h" => DateTime.UtcNow.AddHours(-pointCount + i),
                "7d" => DateTime.UtcNow.AddDays(-pointCount + i),
                "30d" => DateTime.UtcNow.AddDays(-pointCount + i),
                "90d" => DateTime.UtcNow.AddDays(-pointCount + i),
                _ => DateTime.UtcNow.AddHours(-pointCount + i)
            };

            var variation = (random.NextDouble() - 0.5) * 20;
            var value = Math.Max(0, baseValue + variation);

            data.Add(new Dictionary<string, object>
            {
                { "timestamp", timestamp },
                { "value", value }
            });
        }

        return data;
    }

    /// <summary>
    /// Retrieves current system metrics including KPIs and SQL health status
    /// </summary>
    /// <returns>Current metrics with KPIs and SQL health data</returns>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentMetrics()
    {
        try
        {
            var kpiData = await _kpiDataService.GetKpiDataAsync();
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();

            return Ok(new
            {
                kpis = kpiData,
                sql_health = sqlHealth,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics" });
        }
    }

    /// <summary>
    /// Calculates and retrieves business KPIs based on technical metrics
    /// </summary>
    /// <returns>Business KPIs including availability, performance, and reliability metrics</returns>
    [HttpGet("kpis")]
    public async Task<IActionResult> GetBusinessKpis()
    {
        try
        {
            var kpiData = await _kpiDataService.GetKpiDataAsync();
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();
            
            // Combine technical metrics
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
            
            // Calculate business KPIs
            var businessKpis = await _businessKpiService.CalculateBusinessKpisAsync(technicalMetrics);

            return Ok(new
            {
                business_kpis = businessKpis,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business KPIs");
            return StatusCode(500, new { error = "Failed to retrieve business KPIs" });
        }
    }

    /// <summary>
    /// Generates a business impact report based on current system metrics
    /// </summary>
    /// <returns>Business impact report with risk assessment and recommendations</returns>
    [HttpGet("business-impact")]
    public async Task<IActionResult> GetBusinessImpact()
    {
        try
        {
            var kpiData = await _kpiDataService.GetKpiDataAsync();
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();
            
            // Combine technical metrics
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
            
            // Generate business impact report
            var impactReport = await _businessKpiService.GenerateBusinessImpactReportAsync(technicalMetrics);

            return Ok(impactReport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating business impact report");
            return StatusCode(500, new { error = "Failed to generate business impact report" });
        }
    }
}

