using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/bulk-operations")]
public class BulkOperationsController : ControllerBase
{
    private readonly IBatchJobService _batchJobService;
    private readonly ISessionService _sessionService;
    private readonly IAlertService _alertService;
    private readonly ILogger<BulkOperationsController> _logger;

    public BulkOperationsController(
        IBatchJobService batchJobService,
        ISessionService sessionService,
        IAlertService alertService,
        ILogger<BulkOperationsController> logger)
    {
        _batchJobService = batchJobService;
        _sessionService = sessionService;
        _alertService = alertService;
        _logger = logger;
    }

    [HttpPost("batch-jobs/restart")]
    public async Task<IActionResult> BulkRestartBatchJobs([FromBody] BulkOperationRequest request)
    {
        try
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(new { error = "Ids are required" });
            }

            var results = new List<BulkOperationResult>();

            foreach (var id in request.Ids)
            {
                try
                {
                    var success = await _batchJobService.RestartBatchJobAsync(id);
                    results.Add(new BulkOperationResult
                    {
                        Id = id,
                        Success = success,
                        Message = success ? "Restarted successfully" : "Batch job not found"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restarting batch job {Id}", id);
                    results.Add(new BulkOperationResult
                    {
                        Id = id,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return Ok(new
            {
                results = results,
                total = results.Count,
                successful = results.Count(r => r.Success),
                failed = results.Count(r => !r.Success),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk restart batch jobs");
            return StatusCode(500, new { error = "Failed to execute bulk operation" });
        }
    }

    [HttpPost("sessions/kill")]
    public async Task<IActionResult> BulkKillSessions([FromBody] BulkOperationRequest request)
    {
        try
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(new { error = "Ids are required" });
            }

            var results = new List<BulkOperationResult>();

            foreach (var id in request.Ids)
            {
                try
                {
                    var success = await _sessionService.KillSessionAsync(id);
                    results.Add(new BulkOperationResult
                    {
                        Id = id,
                        Success = success,
                        Message = success ? "Killed successfully" : "Session not found"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing session {Id}", id);
                    results.Add(new BulkOperationResult
                    {
                        Id = id,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return Ok(new
            {
                results = results,
                total = results.Count,
                successful = results.Count(r => r.Success),
                failed = results.Count(r => !r.Success),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk kill sessions");
            return StatusCode(500, new { error = "Failed to execute bulk operation" });
        }
    }

    [HttpPost("alerts/resolve")]
    public async Task<IActionResult> BulkResolveAlerts([FromBody] BulkOperationRequest request)
    {
        try
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequest(new { error = "Ids are required" });
            }

            var results = new List<BulkOperationResult>();

            foreach (var id in request.Ids)
            {
                try
                {
                    var success = await _alertService.UpdateAlertStatusAsync(id, "Resolved");
                    results.Add(new BulkOperationResult
                    {
                        Id = id,
                        Success = success,
                        Message = success ? "Resolved successfully" : "Alert not found"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving alert {Id}", id);
                    results.Add(new BulkOperationResult
                    {
                        Id = id,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return Ok(new
            {
                results = results,
                total = results.Count,
                successful = results.Count(r => r.Success),
                failed = results.Count(r => !r.Success),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk resolve alerts");
            return StatusCode(500, new { error = "Failed to execute bulk operation" });
        }
    }
}

public class BulkOperationRequest
{
    public List<int> Ids { get; set; } = new();
}

public class BulkOperationResult
{
    public int Id { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

