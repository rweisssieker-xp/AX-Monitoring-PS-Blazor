using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IArchivingService
{
    Task<int> ArchiveOldDataAsync();
    Task<int> ArchiveBatchJobsAsync(DateTime cutoffDate);
    Task<int> ArchiveSessionsAsync(DateTime cutoffDate);
    Task<int> ArchiveSqlHealthRecordsAsync(DateTime cutoffDate);
    Task<int> ArchiveAlertsAsync(DateTime cutoffDate);
    Task<int> ArchiveBlockingChainsAsync(DateTime cutoffDate);
    Task<ArchivingStats> GetArchivingStatsAsync();
}

public class ArchivingStats
{
    public int ArchivedBatchJobs { get; set; }
    public int ArchivedSessions { get; set; }
    public int ArchivedSqlHealthRecords { get; set; }
    public int ArchivedAlerts { get; set; }
    public int ArchivedBlockingChains { get; set; }
    public DateTime LastArchivingRun { get; set; }
    public TimeSpan ArchivingDuration { get; set; }
}

public class ArchivingService : IArchivingService
{
    private readonly AXDbContext _context;
    private readonly ILogger<ArchivingService> _logger;
    private readonly IConfiguration _configuration;

    public ArchivingService(
        AXDbContext context,
        ILogger<ArchivingService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<int> ArchiveOldDataAsync()
    {
        var startTime = DateTime.UtcNow;
        var totalArchived = 0;

        try
        {
            // Update LastArchivingRun before starting
            await UpdateLastArchivingRunAsync(startTime);
            
            // Get retention periods from configuration (default: 30 days detail, 12 months aggregate)
            var detailRetentionDays = int.Parse(_configuration["DataRetention:DetailRetentionDays"] ?? "30");
            var aggregateRetentionDays = int.Parse(_configuration["DataRetention:AggregateRetentionDays"] ?? "365");

            var detailCutoffDate = DateTime.UtcNow.AddDays(-detailRetentionDays);
            var aggregateCutoffDate = DateTime.UtcNow.AddDays(-aggregateRetentionDays);

            _logger.LogInformation("Starting data archiving: Detail cutoff: {DetailCutoff}, Aggregate cutoff: {AggregateCutoff}", 
                detailCutoffDate, aggregateCutoffDate);

            // Archive detail data (older than 30 days)
            totalArchived += await ArchiveBatchJobsAsync(detailCutoffDate);
            totalArchived += await ArchiveSessionsAsync(detailCutoffDate);
            totalArchived += await ArchiveSqlHealthRecordsAsync(detailCutoffDate);
            totalArchived += await ArchiveBlockingChainsAsync(detailCutoffDate);

            // Archive alerts (older than 30 days, but keep resolved ones longer)
            totalArchived += await ArchiveAlertsAsync(detailCutoffDate);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Data archiving completed: {Count} records archived in {Duration}", 
                totalArchived, duration);

            // Update LastArchivingRun after completion
            await UpdateLastArchivingRunAsync(DateTime.UtcNow);

            return totalArchived;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data archiving");
            throw;
        }
    }

    public async Task<int> ArchiveBatchJobsAsync(DateTime cutoffDate)
    {
        try
        {
            // Archive completed/error/cancelled batch jobs older than cutoff date
            var jobsToArchive = await _context.BatchJobs
                .Where(b => b.CreatedAt < cutoffDate && 
                           (b.Status == "Completed" || b.Status == "Error" || b.Status == "Cancelled"))
                .ToListAsync();

            if (jobsToArchive.Any())
            {
                _context.BatchJobs.RemoveRange(jobsToArchive);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Archived {Count} batch jobs older than {CutoffDate}", 
                    jobsToArchive.Count, cutoffDate);
            }

            return jobsToArchive.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving batch jobs");
            throw;
        }
    }

    public async Task<int> ArchiveSessionsAsync(DateTime cutoffDate)
    {
        try
        {
            // Archive inactive sessions older than cutoff date
            var sessionsToArchive = await _context.Sessions
                .Where(s => s.LoginTime < cutoffDate && 
                           (s.Status == "Inactive" || s.Status == "Closed"))
                .ToListAsync();

            if (sessionsToArchive.Any())
            {
                _context.Sessions.RemoveRange(sessionsToArchive);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Archived {Count} sessions older than {CutoffDate}", 
                    sessionsToArchive.Count, cutoffDate);
            }

            return sessionsToArchive.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving sessions");
            throw;
        }
    }

    public async Task<int> ArchiveSqlHealthRecordsAsync(DateTime cutoffDate)
    {
        try
        {
            // Archive SQL health records older than cutoff date
            var recordsToArchive = await _context.SqlHealthRecords
                .Where(h => h.RecordedAt < cutoffDate)
                .ToListAsync();

            if (recordsToArchive.Any())
            {
                _context.SqlHealthRecords.RemoveRange(recordsToArchive);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Archived {Count} SQL health records older than {CutoffDate}", 
                    recordsToArchive.Count, cutoffDate);
            }

            return recordsToArchive.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving SQL health records");
            throw;
        }
    }

    public async Task<int> ArchiveAlertsAsync(DateTime cutoffDate)
    {
        try
        {
            // Archive resolved alerts older than cutoff date
            var alertsToArchive = await _context.Alerts
                .Where(a => a.CreatedAt < cutoffDate && 
                           (a.Status == "Resolved" || a.Status == "Closed"))
                .ToListAsync();

            if (alertsToArchive.Any())
            {
                _context.Alerts.RemoveRange(alertsToArchive);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Archived {Count} alerts older than {CutoffDate}", 
                    alertsToArchive.Count, cutoffDate);
            }

            return alertsToArchive.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving alerts");
            throw;
        }
    }

    public async Task<int> ArchiveBlockingChainsAsync(DateTime cutoffDate)
    {
        try
        {
            // Archive resolved blocking chains older than cutoff date
            var chainsToArchive = await _context.BlockingChains
                .Where(b => b.DetectedAt < cutoffDate && b.ResolvedAt != null)
                .ToListAsync();

            if (chainsToArchive.Any())
            {
                _context.BlockingChains.RemoveRange(chainsToArchive);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Archived {Count} blocking chains older than {CutoffDate}", 
                    chainsToArchive.Count, cutoffDate);
            }

            return chainsToArchive.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving blocking chains");
            throw;
        }
    }

    public async Task<ArchivingStats> GetArchivingStatsAsync()
    {
        try
        {
            var detailRetentionDays = int.Parse(_configuration["DataRetention:DetailRetentionDays"] ?? "30");
            var cutoffDate = DateTime.UtcNow.AddDays(-detailRetentionDays);

            var stats = new ArchivingStats
            {
                ArchivedBatchJobs = await _context.BatchJobs
                    .CountAsync(b => b.CreatedAt < cutoffDate && 
                                   (b.Status == "Completed" || b.Status == "Error" || b.Status == "Cancelled")),
                ArchivedSessions = await _context.Sessions
                    .CountAsync(s => s.LoginTime < cutoffDate && 
                                   (s.Status == "Inactive" || s.Status == "Closed")),
                ArchivedSqlHealthRecords = await _context.SqlHealthRecords
                    .CountAsync(h => h.RecordedAt < cutoffDate),
                ArchivedAlerts = await _context.Alerts
                    .CountAsync(a => a.CreatedAt < cutoffDate && 
                                   (a.Status == "Resolved" || a.Status == "Closed")),
                ArchivedBlockingChains = await _context.BlockingChains
                    .CountAsync(b => b.DetectedAt < cutoffDate && b.ResolvedAt != null),
                LastArchivingRun = await GetLastArchivingRunAsync(),
                ArchivingDuration = TimeSpan.Zero
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archiving stats");
            throw;
        }
    }

    private async Task UpdateLastArchivingRunAsync(DateTime runTime)
    {
        try
        {
            var setting = await _context.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == "LastArchivingRun");

            if (setting == null)
            {
                setting = new ApplicationSetting
                {
                    Key = "LastArchivingRun",
                    Value = runTime.ToString("O"), // ISO 8601 format
                    Description = "Timestamp of the last archiving run",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ApplicationSettings.Add(setting);
            }
            else
            {
                setting.Value = runTime.ToString("O");
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update LastArchivingRun setting");
            // Don't throw - this is not critical
        }
    }

    private async Task<DateTime> GetLastArchivingRunAsync()
    {
        try
        {
            var setting = await _context.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == "LastArchivingRun");

            if (setting != null && DateTime.TryParse(setting.Value, out var lastRun))
            {
                return lastRun;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve LastArchivingRun setting");
        }

        // Return default if not found
        return DateTime.MinValue;
    }
}

