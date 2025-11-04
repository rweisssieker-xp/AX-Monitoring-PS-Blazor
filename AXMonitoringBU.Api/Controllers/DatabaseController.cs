using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/database")]
public class DatabaseController : ControllerBase
{
    private readonly IKpiDataService _kpiDataService;
    private readonly IBlockingService _blockingService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        IKpiDataService kpiDataService,
        IBlockingService blockingService,
        ILogger<DatabaseController> logger)
    {
        _kpiDataService = kpiDataService;
        _blockingService = blockingService;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();

            return Ok(new
            {
                database_health = sqlHealth,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database health");
            return StatusCode(500, new { error = "Failed to retrieve database health" });
        }
    }

    [HttpGet("blocking")]
    public async Task<IActionResult> GetBlockingChains([FromQuery] bool activeOnly = true)
    {
        try
        {
            var blockingChains = await _blockingService.GetBlockingChainsAsync(activeOnly);

            return Ok(new
            {
                blocking_chains = blockingChains,
                count = blockingChains.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocking chains");
            return StatusCode(500, new { error = "Failed to retrieve blocking chains" });
        }
    }
}

