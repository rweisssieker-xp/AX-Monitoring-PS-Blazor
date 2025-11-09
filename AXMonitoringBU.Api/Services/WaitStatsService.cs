using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IWaitStatsService
{
    Task<List<WaitStat>> GetWaitStatsAsync(int topN = 20);
    Task<List<TopQuery>> GetTopQueriesAsync(int topN = 20, TimeSpan? minDuration = null);
    Task<Dictionary<string, object>> GetWaitStatsSummaryAsync();
}

public class WaitStat
{
    public string WaitType { get; set; } = string.Empty;
    public long WaitCount { get; set; }
    public double WaitTimeMs { get; set; }
    public double MaxWaitTimeMs { get; set; }
    public double SignalWaitTimeMs { get; set; }
    public double Percentage { get; set; }
}

public class TopQuery
{
    public string QueryText { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public long ExecutionCount { get; set; }
    public double TotalDurationMs { get; set; }
    public double AvgDurationMs { get; set; }
    public double MaxDurationMs { get; set; }
    public double MinDurationMs { get; set; }
    public long TotalLogicalReads { get; set; }
    public long TotalPhysicalReads { get; set; }
    public DateTime LastExecutionTime { get; set; }
}

public class WaitStatsService : IWaitStatsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WaitStatsService> _logger;
    private readonly IAXDatabaseService _axDatabaseService;

    public WaitStatsService(
        IConfiguration configuration,
        ILogger<WaitStatsService> logger,
        IAXDatabaseService axDatabaseService)
    {
        _configuration = configuration;
        _logger = logger;
        _axDatabaseService = axDatabaseService;
    }

    public async Task<List<WaitStat>> GetWaitStatsAsync(int topN = 20)
    {
        try
        {
            var waitStats = await _axDatabaseService.GetWaitStatsAsync(topN);
            return waitStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wait stats");
            throw;
        }
    }

    public async Task<List<TopQuery>> GetTopQueriesAsync(int topN = 20, TimeSpan? minDuration = null)
    {
        try
        {
            var minDurationMs = minDuration?.TotalMilliseconds ?? 0;
            var topQueries = await _axDatabaseService.GetTopQueriesAsync(topN, minDurationMs);
            return topQueries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top queries");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetWaitStatsSummaryAsync()
    {
        try
        {
            var waitStats = await GetWaitStatsAsync(20);
            var totalWaitTime = waitStats.Sum(w => w.WaitTimeMs);
            
            var summary = new Dictionary<string, object>
            {
                { "total_wait_time_ms", totalWaitTime },
                { "total_wait_count", waitStats.Sum(w => w.WaitCount) },
                { "top_wait_types", waitStats.Take(5).Select(w => new
                    {
                        w.WaitType,
                        w.WaitTimeMs,
                        w.WaitCount,
                        w.Percentage
                    }).ToList() },
                { "timestamp", DateTime.UtcNow }
            };

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wait stats summary");
            throw;
        }
    }
}

