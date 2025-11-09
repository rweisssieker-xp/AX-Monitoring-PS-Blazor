using AXMonitoringBU.Api.Services;
using Microsoft.Extensions.Hosting;

namespace AXMonitoringBU.Api.BackgroundServices;

public class BaselineRecalculationService : BackgroundService
{
    private readonly ILogger<BaselineRecalculationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval;
    private readonly string _environment;

    private static readonly (string MetricName, string MetricType, string? MetricClass)[] MetricsToRecalculate =
    {
        ("Batch Backlog", "batchduration", null),
        ("Batch Error Rate", "errorrate", null),
        ("Active Sessions", "activesessions", null),
        ("Blocking Chains", "blockingchains", null),
        ("SQL CPU Usage", "cpuusage", null),
        ("SQL Memory Usage", "memoryusage", null)
    };

    public BaselineRecalculationService(
        ILogger<BaselineRecalculationService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var intervalHours = configuration.GetValue<int?>("Baseline:RecalculationIntervalHours") ?? 6;
        _interval = TimeSpan.FromHours(Math.Max(1, intervalHours));
        _environment = configuration["App:Environment"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "PROD";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Baseline recalculation service started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var baselineService = scope.ServiceProvider.GetService<IBaselineService>();
                if (baselineService == null)
                {
                    _logger.LogWarning("IBaselineService not available; skipping baseline recalculation cycle.");
                }
                else
                {
                    foreach (var metric in MetricsToRecalculate)
                    {
                        try
                        {
                            await baselineService.CalculateBaselineAsync(
                                metric.MetricName,
                                metric.MetricType,
                                metric.MetricClass,
                                _environment,
                                windowDays: 14);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error recalculating baseline for {MetricName}/{MetricType}", metric.MetricName, metric.MetricType);
                        }
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unexpected error during baseline recalculation");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Swallow cancellation
            }
        }

        _logger.LogInformation("Baseline recalculation service stopping.");
    }
}
