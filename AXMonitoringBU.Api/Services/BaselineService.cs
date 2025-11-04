using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IBaselineService
{
    Task<Baseline?> GetBaselineAsync(string metricName, string metricType, string? metricClass = null, string environment = "DEV");
    Task<List<Baseline>> GetAllBaselinesAsync(string? environment = null);
    Task<Baseline> CalculateBaselineAsync(string metricName, string metricType, string? metricClass = null, string environment = "DEV", int windowDays = 14);
    Task<bool> IsMetricAboveBaselineAsync(string metricName, string metricType, double currentValue, double thresholdPercent = 30.0, string? metricClass = null, string environment = "DEV");
}

public class BaselineService : IBaselineService
{
    private readonly AXDbContext _context;
    private readonly ILogger<BaselineService> _logger;
    private readonly IConfiguration _configuration;

    public BaselineService(
        AXDbContext context, 
        ILogger<BaselineService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Baseline?> GetBaselineAsync(string metricName, string metricType, string? metricClass = null, string environment = "DEV")
    {
        try
        {
            var query = _context.Baselines
                .Where(b => b.MetricName == metricName 
                    && b.MetricType == metricType 
                    && b.Environment == environment);

            if (!string.IsNullOrEmpty(metricClass))
            {
                query = query.Where(b => b.MetricClass == metricClass);
            }
            else
            {
                query = query.Where(b => b.MetricClass == null);
            }

            return await query
                .OrderByDescending(b => b.BaselineDate)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting baseline for {MetricName}/{MetricType}", metricName, metricType);
            throw;
        }
    }

    public async Task<List<Baseline>> GetAllBaselinesAsync(string? environment = null)
    {
        try
        {
            var query = _context.Baselines.AsQueryable();

            if (!string.IsNullOrEmpty(environment))
            {
                query = query.Where(b => b.Environment == environment);
            }

            return await query
                .OrderByDescending(b => b.BaselineDate)
                .ThenBy(b => b.MetricName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all baselines");
            throw;
        }
    }

    public async Task<Baseline> CalculateBaselineAsync(string metricName, string metricType, string? metricClass = null, string environment = "DEV", int windowDays = 14)
    {
        try
        {
            var windowEnd = DateTime.UtcNow.Date;
            var windowStart = windowEnd.AddDays(-windowDays);

            List<double> values = new List<double>();

            switch (metricType.ToLower())
            {
                case "batchduration":
                    values = await CalculateBatchDurationBaseline(windowStart, windowEnd, metricClass);
                    break;
                case "errorrate":
                    values = await CalculateErrorRateBaseline(windowStart, windowEnd);
                    break;
                case "backlog":
                    values = await CalculateBacklogBaseline(windowStart, windowEnd);
                    break;
                case "activesessions":
                    values = await CalculateActiveSessionsBaseline(windowStart, windowEnd);
                    break;
                case "blockingchains":
                    values = await CalculateBlockingChainsBaseline(windowStart, windowEnd);
                    break;
                case "cpuusage":
                    values = await CalculateCpuUsageBaseline(windowStart, windowEnd);
                    break;
                case "memoryusage":
                    values = await CalculateMemoryUsageBaseline(windowStart, windowEnd);
                    break;
                default:
                    _logger.LogWarning("Unknown metric type: {MetricType}", metricType);
                    throw new ArgumentException($"Unknown metric type: {metricType}");
            }

            if (values.Count == 0)
            {
                throw new InvalidOperationException($"No data available for baseline calculation: {metricName}/{metricType}");
            }

            values.Sort();
            var p50 = CalculatePercentile(values, 0.50);
            var p95 = CalculatePercentile(values, 0.95);
            var p99 = CalculatePercentile(values, 0.99);
            var mean = values.Average();
            var stdDev = CalculateStandardDeviation(values, mean);

            var baseline = new Baseline
            {
                MetricName = metricName,
                MetricType = metricType,
                MetricClass = metricClass,
                Environment = environment,
                Percentile50 = p50,
                Percentile95 = p95,
                Percentile99 = p99,
                Mean = mean,
                StandardDeviation = stdDev,
                SampleCount = values.Count,
                BaselineDate = DateTime.UtcNow,
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Check if baseline already exists
            var existing = await GetBaselineAsync(metricName, metricType, metricClass, environment);
            if (existing != null)
            {
                baseline.Id = existing.Id;
                _context.Baselines.Update(baseline);
            }
            else
            {
                _context.Baselines.Add(baseline);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Calculated baseline for {MetricName}/{MetricType}: P50={P50}, P95={P95}, P99={P99}", 
                metricName, metricType, p50, p95, p99);

            return baseline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating baseline for {MetricName}/{MetricType}", metricName, metricType);
            throw;
        }
    }

    public async Task<bool> IsMetricAboveBaselineAsync(string metricName, string metricType, double currentValue, double thresholdPercent = 30.0, string? metricClass = null, string environment = "DEV")
    {
        try
        {
            var baseline = await GetBaselineAsync(metricName, metricType, metricClass, environment);
            if (baseline == null)
            {
                _logger.LogWarning("No baseline found for {MetricName}/{MetricType}, skipping baseline check", metricName, metricType);
                return false;
            }

            // Use P95 as reference point, add threshold percentage
            var threshold = baseline.Percentile95 * (1 + thresholdPercent / 100.0);
            var isAbove = currentValue > threshold;

            if (isAbove)
            {
                _logger.LogInformation("Metric {MetricName}/{MetricType} exceeds baseline threshold: {CurrentValue} > {Threshold} (P95={P95} + {ThresholdPercent}%)",
                    metricName, metricType, currentValue, threshold, baseline.Percentile95, thresholdPercent);
            }

            return isAbove;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking metric against baseline");
            return false;
        }
    }

    private async Task<List<double>> CalculateBatchDurationBaseline(DateTime start, DateTime end, string? className)
    {
        var jobs = await _context.BatchJobs
            .Where(b => b.StartTime >= start && b.StartTime <= end && b.EndTime != null)
            .ToListAsync();

        if (!string.IsNullOrEmpty(className))
        {
            jobs = jobs.Where(b => b.Name.Contains(className, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return jobs
            .Where(j => j.StartTime.HasValue && j.EndTime.HasValue)
            .Select(j => (j.EndTime!.Value - j.StartTime!.Value).TotalSeconds)
            .ToList();
    }

    private async Task<List<double>> CalculateErrorRateBaseline(DateTime start, DateTime end)
    {
        var dailyRates = new List<double>();
        var currentDate = start;

        while (currentDate <= end)
        {
            var dayStart = currentDate;
            var dayEnd = currentDate.AddDays(1);

            var total = await _context.BatchJobs
                .CountAsync(b => b.CreatedAt >= dayStart && b.CreatedAt < dayEnd);

            var errors = await _context.BatchJobs
                .CountAsync(b => b.CreatedAt >= dayStart && b.CreatedAt < dayEnd && b.Status == "Error");

            if (total > 0)
            {
                dailyRates.Add((errors * 100.0) / total);
            }

            currentDate = currentDate.AddDays(1);
        }

        return dailyRates;
    }

    private async Task<List<double>> CalculateBacklogBaseline(DateTime start, DateTime end)
    {
        var dailyBacklogs = new List<double>();
        var currentDate = start;

        while (currentDate <= end)
        {
            var dayEnd = currentDate.AddDays(1);

            var backlog = await _context.BatchJobs
                .CountAsync(b => b.CreatedAt <= dayEnd && 
                    (b.Status == "Waiting" || b.Status == "Running") &&
                    (b.EndTime == null || b.EndTime > currentDate));

            dailyBacklogs.Add(backlog);
            currentDate = currentDate.AddDays(1);
        }

        return dailyBacklogs;
    }

    private async Task<List<double>> CalculateActiveSessionsBaseline(DateTime start, DateTime end)
    {
        var dailySessions = new List<double>();
        var currentDate = start;

        while (currentDate <= end)
        {
            var dayEnd = currentDate.AddDays(1);

            var sessions = await _context.Sessions
                .CountAsync(s => s.LoginTime <= dayEnd && 
                    (s.LastActivity == null || s.LastActivity > currentDate) &&
                    s.Status == "Active");

            dailySessions.Add(sessions);
            currentDate = currentDate.AddDays(1);
        }

        return dailySessions;
    }

    private async Task<List<double>> CalculateBlockingChainsBaseline(DateTime start, DateTime end)
    {
        var dailyBlockings = new List<double>();
        var currentDate = start;

        while (currentDate <= end)
        {
            var dayEnd = currentDate.AddDays(1);

            var blockings = await _context.BlockingChains
                .CountAsync(b => b.DetectedAt <= dayEnd && 
                    (b.ResolvedAt == null || b.ResolvedAt > currentDate));

            dailyBlockings.Add(blockings);
            currentDate = currentDate.AddDays(1);
        }

        return dailyBlockings;
    }

    private async Task<List<double>> CalculateCpuUsageBaseline(DateTime start, DateTime end)
    {
        var healthRecords = await _context.SqlHealthRecords
            .Where(h => h.RecordedAt >= start && h.RecordedAt <= end)
            .OrderBy(h => h.RecordedAt)
            .Select(h => (double)h.CpuUsage)
            .ToListAsync();

        return healthRecords;
    }

    private async Task<List<double>> CalculateMemoryUsageBaseline(DateTime start, DateTime end)
    {
        var healthRecords = await _context.SqlHealthRecords
            .Where(h => h.RecordedAt >= start && h.RecordedAt <= end)
            .OrderBy(h => h.RecordedAt)
            .Select(h => (double)h.MemoryUsage)
            .ToListAsync();

        return healthRecords;
    }

    private double CalculatePercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        if (sortedValues.Count == 1) return sortedValues[0];

        var index = percentile * (sortedValues.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper) return sortedValues[lower];

        var weight = index - lower;
        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }

    private double CalculateStandardDeviation(List<double> values, double mean)
    {
        if (values.Count == 0) return 0;

        var sumSquaredDiffs = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquaredDiffs / values.Count);
    }
}

