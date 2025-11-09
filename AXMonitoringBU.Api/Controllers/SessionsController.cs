using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for managing user sessions
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IExportService _exportService;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionService sessionService, 
        IExportService exportService,
        ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all sessions, optionally filtered by status
    /// </summary>
    /// <param name="status">Optional status filter (Active, Inactive)</param>
    /// <returns>List of sessions</returns>
    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] string? status = null)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsAsync(status);

            return Ok(new
            {
                sessions = sessions,
                count = sessions.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, new { error = "Failed to retrieve sessions" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(int id)
    {
        var session = await _sessionService.GetSessionByIdAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpPost("{id}/kill")]
    public async Task<IActionResult> KillSession(int id)
    {
        try
        {
            var success = await _sessionService.KillSessionAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new
            {
                message = $"Session {id} termination initiated",
                session_id = id,
                status = "terminating"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing session {SessionId}", id);
            return StatusCode(500, new { error = "Failed to kill session" });
        }
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportSessionsToCsv([FromQuery] string? status = null)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsAsync(status);
            var csvBytes = await _exportService.ExportSessionsToCsvAsync(sessions);
            var filename = $"sessions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sessions to CSV");
            return StatusCode(500, new { error = "Failed to export sessions" });
        }
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportSessionsToExcel([FromQuery] string? status = null)
    {
        try
        {
            var sessions = await _sessionService.GetSessionsAsync(status);
            var excelBytes = await _exportService.ExportSessionsToExcelAsync(sessions);
            var filename = $"sessions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sessions to Excel");
            return StatusCode(500, new { error = "Failed to export sessions" });
        }
    }
}

