using AXMonitoringBU.Api.Services;
using Microsoft.Extensions.Hosting;

namespace AXMonitoringBU.Api.BackgroundServices;

public class DeadlockMonitoringService : BackgroundService
{
    private readonly ILogger<DeadlockMonitoringService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval;

    public DeadlockMonitoringService(
        ILogger<DeadlockMonitoringService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var minutes = configuration.GetValue<int?>("DeadlockCapture:EnsureIntervalMinutes") ?? 5;
        _interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deadlock monitoring background service started. Interval: {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var captureService = scope.ServiceProvider.GetService<IDeadlockCaptureService>();

                if (captureService != null)
                {
                    await captureService.EnsureSessionAsync(stoppingToken);

                    var active = await captureService.IsSessionActiveAsync(stoppingToken);
                    if (!active)
                    {
                        _logger.LogWarning("Deadlock Extended Events session is not active after ensure attempt.");
                    }
                }
                else
                {
                    _logger.LogWarning("IDeadlockCaptureService not registered; deadlock capture cannot be ensured.");
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error ensuring deadlock Extended Events session");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Ignore cancellation
            }
        }

        _logger.LogInformation("Deadlock monitoring background service stopping.");
    }
}
