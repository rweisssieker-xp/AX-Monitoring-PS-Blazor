using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            var status = healthReport.Status == HealthStatus.Healthy ? "healthy" :
                        healthReport.Status == HealthStatus.Degraded ? "degraded" : "unhealthy";

            var response = new
            {
                status = status,
                timestamp = DateTime.UtcNow,
                checks = healthReport.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLower(),
                    description = e.Value.Description,
                    duration_ms = e.Value.Duration.TotalMilliseconds,
                    data = e.Value.Data,
                    exception = e.Value.Exception?.Message
                }).ToList(),
                total_duration_ms = healthReport.TotalDuration.TotalMilliseconds
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 :
                           healthReport.Status == HealthStatus.Degraded ? 200 : 503;

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health status");
            return StatusCode(503, new
            {
                status = "unhealthy",
                error = "Health check failed",
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync(check => 
                check.Tags.Contains("ready"));

            var isReady = healthReport.Status == HealthStatus.Healthy;

            return isReady 
                ? Ok(new { status = "ready", timestamp = DateTime.UtcNow })
                : StatusCode(503, new { status = "not_ready", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking readiness");
            return StatusCode(503, new { status = "not_ready", timestamp = DateTime.UtcNow });
        }
    }

    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        // Liveness check - just verify the service is running
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }
}

