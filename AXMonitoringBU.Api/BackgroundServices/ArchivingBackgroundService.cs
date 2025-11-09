using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.BackgroundServices;

public class ArchivingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ArchivingBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ArchivingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ArchivingBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = int.Parse(_configuration["Archiving:IntervalHours"] ?? "24");
        var interval = TimeSpan.FromHours(intervalHours);

        _logger.LogInformation("Archiving Background Service started. Will run every {IntervalHours} hours", intervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Starting scheduled archiving run");

                using var scope = _serviceProvider.CreateScope();
                var archivingService = scope.ServiceProvider.GetRequiredService<IArchivingService>();

                var archivedCount = await archivingService.ArchiveOldDataAsync();
                
                _logger.LogInformation("Scheduled archiving completed: {Count} records archived", archivedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in archiving background service");
                // Continue running even if one iteration fails
            }
        }

        _logger.LogInformation("Archiving Background Service stopped");
    }
}

