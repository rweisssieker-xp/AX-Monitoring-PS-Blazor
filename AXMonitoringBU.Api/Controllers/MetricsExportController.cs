using Microsoft.AspNetCore.Mvc;
using System.Text;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/metrics")]
public class MetricsExportController : ControllerBase
{
    private readonly ILogger<MetricsExportController> _logger;
    private readonly IKpiDataService _kpiService;
    private readonly IWaitStatsService _waitStatsService;

    public MetricsExportController(
        ILogger<MetricsExportController> logger,
        IKpiDataService kpiService,
        IWaitStatsService waitStatsService)
    {
        _logger = logger;
        _kpiService = kpiService;
        _waitStatsService = waitStatsService;
    }

    [HttpGet("prometheus")]
    public async Task<IActionResult> GetPrometheusMetrics()
    {
        try
        {
            var metrics = new StringBuilder();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

            // Get KPI data
            var kpiData = await _kpiService.GetKpiDataAsync();
            var sqlHealth = await _kpiService.GetSqlHealthAsync();

            // Export KPIs in Prometheus format
            metrics.AppendLine($"# HELP ax_batch_backlog Current batch job backlog");
            metrics.AppendLine($"# TYPE ax_batch_backlog gauge");
            metrics.AppendLine($"ax_batch_backlog {kpiData.GetValueOrDefault("batch_backlog", 0)} {timestamp}");

            metrics.AppendLine($"# HELP ax_error_rate Batch job error rate percentage");
            metrics.AppendLine($"# TYPE ax_error_rate gauge");
            metrics.AppendLine($"ax_error_rate {kpiData.GetValueOrDefault("error_rate", 0.0)} {timestamp}");

            metrics.AppendLine($"# HELP ax_active_sessions Number of active sessions");
            metrics.AppendLine($"# TYPE ax_active_sessions gauge");
            metrics.AppendLine($"ax_active_sessions {kpiData.GetValueOrDefault("active_sessions", 0)} {timestamp}");

            metrics.AppendLine($"# HELP ax_blocking_chains Number of active blocking chains");
            metrics.AppendLine($"# TYPE ax_blocking_chains gauge");
            metrics.AppendLine($"ax_blocking_chains {kpiData.GetValueOrDefault("blocking_chains", 0)} {timestamp}");

            // Export SQL Health metrics
            metrics.AppendLine($"# HELP ax_sql_cpu_usage SQL Server CPU usage percentage");
            metrics.AppendLine($"# TYPE ax_sql_cpu_usage gauge");
            metrics.AppendLine($"ax_sql_cpu_usage {sqlHealth.GetValueOrDefault("cpu_usage", 0.0)} {timestamp}");

            metrics.AppendLine($"# HELP ax_sql_memory_usage SQL Server memory usage percentage");
            metrics.AppendLine($"# TYPE ax_sql_memory_usage gauge");
            metrics.AppendLine($"ax_sql_memory_usage {sqlHealth.GetValueOrDefault("memory_usage", 0.0)} {timestamp}");

            metrics.AppendLine($"# HELP ax_sql_io_wait SQL Server IO wait time");
            metrics.AppendLine($"# TYPE ax_sql_io_wait gauge");
            metrics.AppendLine($"ax_sql_io_wait {sqlHealth.GetValueOrDefault("io_wait", 0.0)} {timestamp}");

            metrics.AppendLine($"# HELP ax_sql_tempdb_usage TempDB usage percentage");
            metrics.AppendLine($"# TYPE ax_sql_tempdb_usage gauge");
            metrics.AppendLine($"ax_sql_tempdb_usage {sqlHealth.GetValueOrDefault("tempdb_usage", 0.0)} {timestamp}");

            // Get Wait Stats
            var waitStats = await _waitStatsService.GetWaitStatsAsync(10);
            foreach (var waitStat in waitStats)
            {
                var waitType = waitStat.WaitType.Replace("-", "_").Replace(" ", "_").ToLower();
                metrics.AppendLine($"# HELP ax_wait_time_ms_{waitType} Wait time in milliseconds for {waitStat.WaitType}");
                metrics.AppendLine($"# TYPE ax_wait_time_ms_{waitType} gauge");
                metrics.AppendLine($"ax_wait_time_ms_{waitType} {waitStat.WaitTimeMs} {timestamp}");
            }

            return Content(metrics.ToString(), "text/plain; version=0.0.4");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Prometheus metrics");
            return StatusCode(500, new { error = "Failed to export Prometheus metrics" });
        }
    }
}

