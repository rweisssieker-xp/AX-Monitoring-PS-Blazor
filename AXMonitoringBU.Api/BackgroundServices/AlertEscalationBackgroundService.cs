using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.BackgroundServices;

/// <summary>
/// Background service that periodically checks for alerts that need escalation
/// </summary>
public class AlertEscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertEscalationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public AlertEscalationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AlertEscalationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert Escalation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var escalationService = scope.ServiceProvider.GetRequiredService<IAlertEscalationService>();

                await escalationService.CheckAndEscalateAlertsAsync();

                _logger.LogDebug("Completed escalation check cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alert escalation background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Alert Escalation Background Service stopped");
    }
}

