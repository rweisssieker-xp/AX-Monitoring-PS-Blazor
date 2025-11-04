using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/batch-jobs")]
public class BatchJobsController : ControllerBase
{
    private readonly IBatchJobService _batchJobService;
    private readonly ILogger<BatchJobsController> _logger;

    public BatchJobsController(IBatchJobService batchJobService, ILogger<BatchJobsController> logger)
    {
        _batchJobService = batchJobService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBatchJobs([FromQuery] string? status = null)
    {
        try
        {
            var batchJobs = await _batchJobService.GetBatchJobsAsync(status);

            return Ok(new
            {
                batch_jobs = batchJobs,
                count = batchJobs.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch jobs");
            return StatusCode(500, new { error = "Failed to retrieve batch jobs" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBatchJob(int id)
    {
        var batchJob = await _batchJobService.GetBatchJobByIdAsync(id);
        if (batchJob == null)
        {
            return NotFound();
        }

        return Ok(batchJob);
    }

    [HttpPost("{id}/restart")]
    public async Task<IActionResult> RestartBatchJob(int id)
    {
        try
        {
            var success = await _batchJobService.RestartBatchJobAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new
            {
                message = $"Batch job {id} restart initiated",
                job_id = id,
                status = "restarting"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting batch job {BatchJobId}", id);
            return StatusCode(500, new { error = "Failed to restart batch job" });
        }
    }
}

