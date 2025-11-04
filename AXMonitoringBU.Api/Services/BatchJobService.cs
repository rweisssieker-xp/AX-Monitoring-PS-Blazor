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
    private readonly ILogger<BatchJobService> _logger;

    public BatchJobService(AXDbContext context, ILogger<BatchJobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<BatchJob>> GetBatchJobsAsync(string? status = null)
    {
        try
        {
            var query = _context.BatchJobs.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            return await query
                .OrderByDescending(b => b.StartTime ?? b.CreatedAt)
                .ToListAsync();
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
            return await _context.BatchJobs.FindAsync(id);
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
            var batchJob = await _context.BatchJobs.FindAsync(id);
            if (batchJob == null)
            {
                return false;
            }

            // TODO: Implement actual restart logic (call AX API)
            batchJob.Status = "Waiting";
            batchJob.Progress = 0;
            batchJob.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting batch job {BatchJobId}", id);
            throw;
        }
    }
}

