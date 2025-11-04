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
}

