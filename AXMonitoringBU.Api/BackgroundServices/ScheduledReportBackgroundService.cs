using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.BackgroundServices;

public class ScheduledReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledReportBackgroundService> _logger;

    public ScheduledReportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScheduledReportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check every hour for due reports
        var interval = TimeSpan.FromHours(1);

        _logger.LogInformation("Scheduled Report Background Service started. Will check every {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogDebug("Checking for due scheduled reports");

                using var scope = _serviceProvider.CreateScope();
                var scheduledReportService = scope.ServiceProvider.GetRequiredService<IScheduledReportService>();

                await scheduledReportService.ExecuteDueReportsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled report background service");
                // Continue running even if one iteration fails
            }
        }

        _logger.LogInformation("Scheduled Report Background Service stopped");
    }
}

