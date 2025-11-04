using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IBatchJobService
{
    Task<IEnumerable<BatchJob>> GetBatchJobsAsync(string? status = null);
    Task<BatchJob?> GetBatchJobByIdAsync(int id);
    Task<bool> RestartBatchJobAsync(int id);
}

public class BatchJobService : IBatchJobService
{
    private readonly AXDbContext _context;
    private readonly IAXDatabaseService _axDatabaseService;
    private readonly ILogger<BatchJobService> _logger;

    public BatchJobService(
        AXDbContext context, 
        IAXDatabaseService axDatabaseService,
        ILogger<BatchJobService> logger)
    {
        _context = context;
        _axDatabaseService = axDatabaseService;
        _logger = logger;
    }

    public async Task<IEnumerable<BatchJob>> GetBatchJobsAsync(string? status = null)
    {
        try
        {
            // Read directly from AX database
            var axBatchJobs = await _axDatabaseService.GetBatchJobsFromAXAsync(status);
            
            // Optionally sync to local monitoring database for history
            // For now, just return the AX data
            return axBatchJobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch jobs");
            throw;
        }
    }

    public async Task<BatchJob?> GetBatchJobByIdAsync(int id)
    {
        try
        {
            // Try to find in local database first
            var localBatchJob = await _context.BatchJobs.FindAsync(id);
            if (localBatchJob != null)
            {
                return localBatchJob;
            }

            // If not found, get from AX database
            var axBatchJobs = await _axDatabaseService.GetBatchJobsFromAXAsync();
            return axBatchJobs.FirstOrDefault(b => b.BatchJobId == id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch job by id {BatchJobId}", id);
            throw;
        }
    }

    public async Task<bool> RestartBatchJobAsync(int id)
    {
        try
        {
            // First try to get the batch job
            var batchJob = await GetBatchJobByIdAsync(id);
            if (batchJob == null)
            {
                _logger.LogWarning("Batch job {BatchJobId} not found", id);
                return false;
            }

            // Try to restart in AX database if BatchJobId (RECID) is available
            if (!string.IsNullOrEmpty(batchJob.BatchJobId))
            {
                var axRestarted = await _axDatabaseService.RestartBatchJobInAXAsync(batchJob.BatchJobId);
                if (axRestarted)
                {
                    _logger.LogInformation("Batch job {BatchJobId} restarted in AX database", id);
                    return true;
                }
            }

            // Fallback: Update local monitoring database
            var localBatchJob = await _context.BatchJobs.FindAsync(id);
            if (localBatchJob != null)
            {
                localBatchJob.Status = "Waiting";
                localBatchJob.Progress = 0;
                localBatchJob.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Batch job {BatchJobId} status updated in local database", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting batch job {BatchJobId}", id);
            throw;
        }
    }
}

