using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/archiving")]
public class ArchivingController : ControllerBase
{
    private readonly IArchivingService _archivingService;
    private readonly ILogger<ArchivingController> _logger;

    public ArchivingController(
        IArchivingService archivingService,
        ILogger<ArchivingController> logger)
    {
        _archivingService = archivingService;
        _logger = logger;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunArchiving()
    {
        try
        {
            var archivedCount = await _archivingService.ArchiveOldDataAsync();
            return Ok(new
            {
                message = "Archiving completed successfully",
                archived_count = archivedCount,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running archiving");
            return StatusCode(500, new { error = "Failed to run archiving" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetArchivingStats()
    {
        try
        {
            var stats = await _archivingService.GetArchivingStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archiving stats");
            return StatusCode(500, new { error = "Failed to retrieve archiving stats" });
        }
    }
}

