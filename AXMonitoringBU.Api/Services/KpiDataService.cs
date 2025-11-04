using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IKpiDataService
{
    Task<Dictionary<string, object>> GetKpiDataAsync();
    Task<Dictionary<string, object>> GetSqlHealthAsync();
}

public class KpiDataService : IKpiDataService
{
    private readonly AXDbContext _context;
    private readonly ILogger<KpiDataService> _logger;

    public KpiDataService(AXDbContext context, ILogger<KpiDataService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> GetKpiDataAsync()
    {
        try
        {
            var batchBacklog = await _context.BatchJobs
                .CountAsync(b => b.Status == "Waiting" || b.Status == "Running");

            var totalBatches = await _context.BatchJobs.CountAsync();
            var errorBatches = await _context.BatchJobs.CountAsync(b => b.Status == "Error");
            var errorRate = totalBatches > 0 ? (errorBatches * 100.0 / totalBatches) : 0;

            var activeSessions = await _context.Sessions
                .CountAsync(s => s.Status == "Active");

            var blockingChains = await _context.BlockingChains
                .CountAsync(b => b.ResolvedAt == null);

            return new Dictionary<string, object>
            {
                { "batch_backlog", batchBacklog },
                { "error_rate", Math.Round(errorRate, 1) },
                { "active_sessions", activeSessions },
                { "blocking_chains", blockingChains }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KPI data");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetSqlHealthAsync()
    {
        try
        {
            var latestHealth = await _context.SqlHealthRecords
                .OrderByDescending(h => h.RecordedAt)
                .FirstOrDefaultAsync();

            if (latestHealth == null)
            {
                return new Dictionary<string, object>
                {
                    { "cpu_usage", 0.0 },
                    { "memory_usage", 0.0 },
                    { "io_wait", 0.0 },
                    { "tempdb_usage", 0.0 },
                    { "active_connections", 0 },
                    { "longest_running_query", 0 }
                };
            }

            return new Dictionary<string, object>
            {
                { "cpu_usage", latestHealth.CpuUsage },
                { "memory_usage", latestHealth.MemoryUsage },
                { "io_wait", latestHealth.IoWait },
                { "tempdb_usage", latestHealth.TempDbUsage },
                { "active_connections", latestHealth.ActiveConnections },
                { "longest_running_query", latestHealth.LongestRunningQueryMinutes }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SQL health data");
            throw;
        }
    }
}

