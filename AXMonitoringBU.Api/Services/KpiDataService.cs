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
    private readonly IAXDatabaseService _axDatabaseService;
    private readonly ILogger<KpiDataService> _logger;

    public KpiDataService(
        AXDbContext context, 
        IAXDatabaseService axDatabaseService,
        ILogger<KpiDataService> logger)
    {
        _context = context;
        _axDatabaseService = axDatabaseService;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> GetKpiDataAsync()
    {
        try
        {
            // Get batch backlog directly from AX database
            var batchBacklog = await _axDatabaseService.GetBatchBacklogCountAsync();
            
            // Get active sessions directly from AX database
            var activeSessions = await _axDatabaseService.GetActiveSessionsCountAsync();

            // Get error rate directly from AX database
            var errorRate = await _axDatabaseService.GetBatchErrorRateAsync();

            // Get blocking chains from local database (or could be from AX if available)
            var blockingChains = await _context.BlockingChains
                .CountAsync(b => b.ResolvedAt == null);

            return new Dictionary<string, object>
            {
                { "batch_backlog", batchBacklog },
                { "error_rate", errorRate },
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
            // Read directly from SQL Server DMVs via AX database connection
            var sqlHealth = await _axDatabaseService.GetSqlHealthFromDMVsAsync();
            
            // Optionally save to local database for history
            if (sqlHealth != null && sqlHealth.Any())
            {
                try
                {
                    var healthRecord = new SqlHealth
                    {
                        CpuUsage = Convert.ToDouble(sqlHealth["cpu_usage"]),
                        MemoryUsage = Convert.ToDouble(sqlHealth["memory_usage"]),
                        IoWait = Convert.ToDouble(sqlHealth["io_wait"]),
                        TempDbUsage = Convert.ToDouble(sqlHealth["tempdb_usage"]),
                        ActiveConnections = Convert.ToInt32(sqlHealth["active_connections"]),
                        LongestRunningQueryMinutes = Convert.ToInt32(sqlHealth["longest_running_query"]),
                        RecordedAt = DateTime.UtcNow
                    };
                    
                    _context.SqlHealthRecords.Add(healthRecord);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error saving SQL health to local database");
                }
            }

            return sqlHealth ?? new Dictionary<string, object>
            {
                { "cpu_usage", 0.0 },
                { "memory_usage", 0.0 },
                { "io_wait", 0.0 },
                { "tempdb_usage", 0.0 },
                { "active_connections", 0 },
                { "longest_running_query", 0 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SQL health data");
            // Return default values on error
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
    }
}

