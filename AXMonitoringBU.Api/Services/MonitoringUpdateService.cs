using Microsoft.AspNetCore.SignalR;
using AXMonitoringBU.Api.Hubs;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Services;

public class MonitoringUpdateService : BackgroundService
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoringUpdateService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(30); // Configurable

    public MonitoringUpdateService(
        IHubContext<MonitoringHub> hubContext,
        IServiceProvider serviceProvider,
        ILogger<MonitoringUpdateService> logger)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonitoringUpdateService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendUpdatesAsync();
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending monitoring updates");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait before retry
            }
        }

        _logger.LogInformation("MonitoringUpdateService stopped");
    }

    private async Task SendUpdatesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var kpiService = scope.ServiceProvider.GetRequiredService<IKpiDataService>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

        try
        {
            // Send KPI updates
            var kpiData = await kpiService.GetKpiDataAsync();
            var sqlHealth = await kpiService.GetSqlHealthAsync();

            if (kpiData != null && sqlHealth != null)
            {
                await _hubContext.Clients.All.SendAsync("KpiUpdated", new
                {
                    kpis = kpiData,
                    sql_health = sqlHealth,
                    timestamp = DateTime.UtcNow
                });

                await CheckBaselineAlertsAsync(alertService, kpiData, sqlHealth);
            }

            // Send active alerts count
            var activeAlerts = await alertService.GetActiveAlertsAsync();
            if (activeAlerts != null)
            {
                var alertsList = activeAlerts.ToList();
                await _hubContext.Clients.All.SendAsync("AlertsUpdated", new
                {
                    active_count = alertsList.Count,
                    critical_count = alertsList.Count(a => a.Severity == "Critical"),
                    alerts = alertsList.Take(10).Select(a => new
                    {
                        id = a.Id,
                        alertId = a.AlertId,
                        type = a.Type,
                        severity = a.Severity,
                        message = a.Message,
                        status = a.Status,
                        timestamp = a.Timestamp
                    }).ToList(),
                    timestamp = DateTime.UtcNow
                });
            }

            // Send system status
            await _hubContext.Clients.All.SendAsync("SystemStatusUpdated", new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendUpdatesAsync");
        }
    }

    private static async Task CheckBaselineAlertsAsync(IAlertService alertService, Dictionary<string, object> kpiData, Dictionary<string, object> sqlHealth)
    {
        static double ToDouble(object? value)
        {
            if (value == null)
            {
                return 0;
            }

            return value switch
            {
                double d => d,
                float f => f,
                decimal m => (double)m,
                int i => i,
                long l => l,
                _ => double.TryParse(value.ToString(), out var parsed) ? parsed : 0
            };
        }

        var backlog = ToDouble(kpiData.GetValueOrDefault("batch_backlog"));
        if (backlog > 0)
        {
            await alertService.CheckBaselineAndCreateAlertAsync("Batch Backlog", "backlog", backlog);
        }

        var errorRate = ToDouble(kpiData.GetValueOrDefault("error_rate"));
        if (errorRate > 0)
        {
            await alertService.CheckBaselineAndCreateAlertAsync("Batch Error Rate", "errorrate", errorRate, 20);
        }

        var activeSessions = ToDouble(kpiData.GetValueOrDefault("active_sessions"));
        if (activeSessions > 0)
        {
            await alertService.CheckBaselineAndCreateAlertAsync("Active Sessions", "activesessions", activeSessions, 25);
        }

        var blockingChains = ToDouble(kpiData.GetValueOrDefault("blocking_chains"));
        if (blockingChains > 0)
        {
            await alertService.CheckBaselineAndCreateAlertAsync("Blocking Chains", "blockingchains", blockingChains);
        }

        var cpuUsage = ToDouble(sqlHealth.GetValueOrDefault("cpu_usage"));
        if (cpuUsage > 0)
        {
            await alertService.CheckBaselineAndCreateAlertAsync("SQL CPU Usage", "cpuusage", cpuUsage, 15);
        }

        var memoryUsage = ToDouble(sqlHealth.GetValueOrDefault("memory_usage"));
        if (memoryUsage > 0)
        {
            await alertService.CheckBaselineAndCreateAlertAsync("SQL Memory Usage", "memoryusage", memoryUsage, 15);
        }
    }
}

