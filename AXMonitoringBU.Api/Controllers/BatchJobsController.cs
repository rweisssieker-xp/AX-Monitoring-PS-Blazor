using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/batch-jobs")]
public class BatchJobsController : ControllerBase
{
    private readonly IBatchJobService _batchJobService;
    private readonly IExportService _exportService;
    private readonly ILogger<BatchJobsController> _logger;

    public BatchJobsController(
        IBatchJobService batchJobService, 
        IExportService exportService,
        ILogger<BatchJobsController> logger)
    {
        _batchJobService = batchJobService;
        _exportService = exportService;
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

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportBatchJobsToCsv([FromQuery] string? status = null)
    {
        try
        {
            var batchJobs = await _batchJobService.GetBatchJobsAsync(status);
            var csvBytes = await _exportService.ExportBatchJobsToCsvAsync(batchJobs);
            var filename = $"batch_jobs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting batch jobs to CSV");
            return StatusCode(500, new { error = "Failed to export batch jobs" });
        }
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportBatchJobsToExcel([FromQuery] string? status = null)
    {
        try
        {
            var batchJobs = await _batchJobService.GetBatchJobsAsync(status);
            var excelBytes = await _exportService.ExportBatchJobsToExcelAsync(batchJobs);
            var filename = $"batch_jobs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting batch jobs to Excel");
            return StatusCode(500, new { error = "Failed to export batch jobs" });
        }
    }
}

