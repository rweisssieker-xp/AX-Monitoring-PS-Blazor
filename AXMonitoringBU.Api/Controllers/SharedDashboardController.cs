using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for managing shared dashboards
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/dashboards/shared")]
public class SharedDashboardController : ControllerBase
{
    private readonly ISharedDashboardService _dashboardService;
    private readonly ILogger<SharedDashboardController> _logger;

    public SharedDashboardController(
        ISharedDashboardService dashboardService,
        ILogger<SharedDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get all dashboards accessible to the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboards([FromQuery] bool includePublic = true)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            var dashboards = await _dashboardService.GetDashboardsForUserAsync(username, includePublic);
            return Ok(new { dashboards, count = dashboards.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboards");
            return StatusCode(500, new { error = "Failed to retrieve dashboards" });
        }
    }

    /// <summary>
    /// Get team dashboards
    /// </summary>
    [HttpGet("teams/{teamName}")]
    public async Task<IActionResult> GetTeamDashboards(string teamName)
    {
        try
        {
            var dashboards = await _dashboardService.GetTeamDashboardsAsync(teamName);
            return Ok(new { dashboards, count = dashboards.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team dashboards for {TeamName}", teamName);
            return StatusCode(500, new { error = "Failed to retrieve team dashboards" });
        }
    }

    /// <summary>
    /// Get public dashboards
    /// </summary>
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicDashboards()
    {
        try
        {
            var dashboards = await _dashboardService.GetPublicDashboardsAsync();
            return Ok(new { dashboards, count = dashboards.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public dashboards");
            return StatusCode(500, new { error = "Failed to retrieve public dashboards" });
        }
    }

    /// <summary>
    /// Get dashboard by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDashboard(int id)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            
            if (!await _dashboardService.HasAccessAsync(id, username))
            {
                return Forbid();
            }

            var dashboard = await _dashboardService.GetDashboardByIdAsync(id);
            if (dashboard == null)
            {
                return NotFound();
            }

            // Record access
            await _dashboardService.RecordAccessAsync(id);

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard {DashboardId}", id);
            return StatusCode(500, new { error = "Failed to retrieve dashboard" });
        }
    }

    /// <summary>
    /// Create a new dashboard
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDashboard([FromBody] SharedDashboard dashboard)
    {
        try
        {
            dashboard.CreatedBy = User.Identity?.Name ?? "Anonymous";
            var created = await _dashboardService.CreateDashboardAsync(dashboard);
            return CreatedAtAction(nameof(GetDashboard), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dashboard");
            return StatusCode(500, new { error = "Failed to create dashboard" });
        }
    }

    /// <summary>
    /// Update a dashboard
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDashboard(int id, [FromBody] SharedDashboard dashboard)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            
            if (!await _dashboardService.CanEditAsync(id, username))
            {
                return Forbid();
            }

            var success = await _dashboardService.UpdateDashboardAsync(id, dashboard);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Dashboard updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard {DashboardId}", id);
            return StatusCode(500, new { error = "Failed to update dashboard" });
        }
    }

    /// <summary>
    /// Delete a dashboard
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDashboard(int id)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            var dashboard = await _dashboardService.GetDashboardByIdAsync(id);
            
            if (dashboard == null)
            {
                return NotFound();
            }

            if (dashboard.CreatedBy != username)
            {
                return Forbid();
            }

            var success = await _dashboardService.DeleteDashboardAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Dashboard deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dashboard {DashboardId}", id);
            return StatusCode(500, new { error = "Failed to delete dashboard" });
        }
    }

    /// <summary>
    /// Share a dashboard with a user
    /// </summary>
    [HttpPost("{id}/share")]
    public async Task<IActionResult> ShareDashboard(int id, [FromBody] ShareDashboardDto dto)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            var dashboard = await _dashboardService.GetDashboardByIdAsync(id);
            
            if (dashboard == null)
            {
                return NotFound();
            }

            if (dashboard.CreatedBy != username)
            {
                return Forbid();
            }

            var success = await _dashboardService.ShareDashboardAsync(id, dto.SharedWith, dto.Permission ?? "view", username);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to share dashboard" });
            }

            return Ok(new { message = "Dashboard shared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing dashboard {DashboardId}", id);
            return StatusCode(500, new { error = "Failed to share dashboard" });
        }
    }

    /// <summary>
    /// Unshare a dashboard with a user
    /// </summary>
    [HttpDelete("{id}/share/{sharedWith}")]
    public async Task<IActionResult> UnshareDashboard(int id, string sharedWith)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            var dashboard = await _dashboardService.GetDashboardByIdAsync(id);
            
            if (dashboard == null)
            {
                return NotFound();
            }

            if (dashboard.CreatedBy != username)
            {
                return Forbid();
            }

            var success = await _dashboardService.UnshareDashboardAsync(id, sharedWith);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Dashboard unshared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsharing dashboard {DashboardId}", id);
            return StatusCode(500, new { error = "Failed to unshare dashboard" });
        }
    }

    /// <summary>
    /// Get shares for a dashboard
    /// </summary>
    [HttpGet("{id}/shares")]
    public async Task<IActionResult> GetDashboardShares(int id)
    {
        try
        {
            var username = User.Identity?.Name ?? "Anonymous";
            
            if (!await _dashboardService.HasAccessAsync(id, username))
            {
                return Forbid();
            }

            var shares = await _dashboardService.GetDashboardSharesAsync(id);
            return Ok(new { shares, count = shares.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shares for dashboard {DashboardId}", id);
            return StatusCode(500, new { error = "Failed to retrieve shares" });
        }
    }
}

public class ShareDashboardDto
{
    public string SharedWith { get; set; } = string.Empty;
    public string? Permission { get; set; } // "view", "edit", "admin"
}

