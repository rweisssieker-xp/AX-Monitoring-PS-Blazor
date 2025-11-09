using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.BackgroundServices;

/// <summary>
/// Background service that periodically correlates alerts to identify incidents
/// </summary>
public class AlertCorrelationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlertCorrelationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes

    public AlertCorrelationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AlertCorrelationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert Correlation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var correlationService = scope.ServiceProvider.GetRequiredService<IAlertCorrelationService>();

                var correlation = await correlationService.CorrelateAlertsAsync();
                if (correlation != null)
                {
                    _logger.LogInformation("Created correlation {CorrelationId} with {AlertCount} alerts", 
                        correlation.CorrelationId, correlation.AlertCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alert correlation background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Alert Correlation Background Service stopped");
    }
}

