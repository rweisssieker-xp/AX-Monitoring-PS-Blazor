using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.BackgroundServices;

/// <summary>
/// Background service that periodically checks cost budgets and sends alerts
/// </summary>
public class CostBudgetBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CostBudgetBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public CostBudgetBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CostBudgetBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cost Budget Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var costService = scope.ServiceProvider.GetRequiredService<ICostTrackingService>();

                await costService.CheckBudgetAlertsAsync();

                _logger.LogDebug("Completed cost budget check cycle");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cost budget background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Cost Budget Background Service stopped");
    }
}

