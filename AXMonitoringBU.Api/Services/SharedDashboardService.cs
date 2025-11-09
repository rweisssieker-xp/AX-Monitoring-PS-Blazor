using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;
using System.Text.Json;

namespace AXMonitoringBU.Api.Services;

public interface ISharedDashboardService
{
    Task<IEnumerable<SharedDashboard>> GetDashboardsForUserAsync(string username, bool includePublic = true);
    Task<IEnumerable<SharedDashboard>> GetTeamDashboardsAsync(string teamName);
    Task<IEnumerable<SharedDashboard>> GetPublicDashboardsAsync();
    Task<SharedDashboard?> GetDashboardByIdAsync(int id);
    Task<SharedDashboard?> GetDashboardByDashboardIdAsync(string dashboardId);
    Task<SharedDashboard> CreateDashboardAsync(SharedDashboard dashboard);
    Task<bool> UpdateDashboardAsync(int id, SharedDashboard dashboard);
    Task<bool> DeleteDashboardAsync(int id);
    Task<bool> ShareDashboardAsync(int dashboardId, string sharedWith, string permission, string sharedBy);
    Task<bool> UnshareDashboardAsync(int dashboardId, string sharedWith);
    Task<IEnumerable<DashboardShare>> GetDashboardSharesAsync(int dashboardId);
    Task<bool> HasAccessAsync(int dashboardId, string username);
    Task<bool> CanEditAsync(int dashboardId, string username);
    Task RecordAccessAsync(int dashboardId);
}

public class SharedDashboardService : ISharedDashboardService
{
    private readonly AXDbContext _context;
    private readonly ILogger<SharedDashboardService> _logger;

    public SharedDashboardService(
        AXDbContext context,
        ILogger<SharedDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<SharedDashboard>> GetDashboardsForUserAsync(string username, bool includePublic = true)
    {
        try
        {
            var query = _context.SharedDashboards.AsQueryable();

            // Get dashboards created by user
            var userDashboards = query.Where(d => d.CreatedBy == username);

            // Get dashboards shared with user
            var sharedDashboardIds = await _context.DashboardShares
                .Where(s => s.SharedWith == username)
                .Select(s => s.DashboardId)
                .ToListAsync();
            var sharedDashboards = query.Where(d => sharedDashboardIds.Contains(d.Id));

            // Get public dashboards if requested
            var publicDashboards = includePublic 
                ? query.Where(d => d.IsPublic && d.CreatedBy != username)
                : Enumerable.Empty<SharedDashboard>();

            // Combine and return unique dashboards
            var allDashboards = await userDashboards
                .Union(sharedDashboards)
                .Union(publicDashboards)
                .OrderByDescending(d => d.LastAccessedAt ?? d.CreatedAt)
                .ToListAsync();

            return allDashboards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboards for user {Username}", username);
            throw;
        }
    }

    public async Task<IEnumerable<SharedDashboard>> GetTeamDashboardsAsync(string teamName)
    {
        try
        {
            return await _context.SharedDashboards
                .Where(d => d.IsTeamWorkspace && d.TeamName == teamName)
                .OrderByDescending(d => d.LastAccessedAt ?? d.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team dashboards for {TeamName}", teamName);
            throw;
        }
    }

    public async Task<IEnumerable<SharedDashboard>> GetPublicDashboardsAsync()
    {
        try
        {
            return await _context.SharedDashboards
                .Where(d => d.IsPublic)
                .OrderByDescending(d => d.AccessCount)
                .ThenByDescending(d => d.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public dashboards");
            throw;
        }
    }

    public async Task<SharedDashboard?> GetDashboardByIdAsync(int id)
    {
        try
        {
            return await _context.SharedDashboards.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard {DashboardId}", id);
            throw;
        }
    }

    public async Task<SharedDashboard?> GetDashboardByDashboardIdAsync(string dashboardId)
    {
        try
        {
            return await _context.SharedDashboards
                .FirstOrDefaultAsync(d => d.DashboardId == dashboardId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard by DashboardId {DashboardId}", dashboardId);
            throw;
        }
    }

    public async Task<SharedDashboard> CreateDashboardAsync(SharedDashboard dashboard)
    {
        try
        {
            if (string.IsNullOrEmpty(dashboard.DashboardId))
            {
                dashboard.DashboardId = $"DASH_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
            }

            dashboard.CreatedAt = DateTime.UtcNow;
            _context.SharedDashboards.Add(dashboard);
            await _context.SaveChangesAsync();
            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dashboard");
            throw;
        }
    }

    public async Task<bool> UpdateDashboardAsync(int id, SharedDashboard dashboard)
    {
        try
        {
            var existing = await _context.SharedDashboards.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            existing.Name = dashboard.Name;
            existing.Description = dashboard.Description;
            existing.LayoutJson = dashboard.LayoutJson;
            existing.IsPublic = dashboard.IsPublic;
            existing.IsTeamWorkspace = dashboard.IsTeamWorkspace;
            existing.TeamName = dashboard.TeamName;
            existing.IsDefault = dashboard.IsDefault;
            existing.Tags = dashboard.Tags;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard {DashboardId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteDashboardAsync(int id)
    {
        try
        {
            var dashboard = await _context.SharedDashboards.FindAsync(id);
            if (dashboard == null)
            {
                return false;
            }

            // Delete shares first
            var shares = await _context.DashboardShares
                .Where(s => s.DashboardId == id)
                .ToListAsync();
            _context.DashboardShares.RemoveRange(shares);

            _context.SharedDashboards.Remove(dashboard);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dashboard {DashboardId}", id);
            throw;
        }
    }

    public async Task<bool> ShareDashboardAsync(int dashboardId, string sharedWith, string permission, string sharedBy)
    {
        try
        {
            // Check if already shared
            var existing = await _context.DashboardShares
                .FirstOrDefaultAsync(s => s.DashboardId == dashboardId && s.SharedWith == sharedWith);

            if (existing != null)
            {
                existing.Permission = permission;
                existing.SharedBy = sharedBy;
                existing.SharedAt = DateTime.UtcNow;
            }
            else
            {
                var share = new DashboardShare
                {
                    DashboardId = dashboardId,
                    SharedWith = sharedWith,
                    Permission = permission,
                    SharedBy = sharedBy,
                    SharedAt = DateTime.UtcNow
                };
                _context.DashboardShares.Add(share);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing dashboard {DashboardId} with {SharedWith}", dashboardId, sharedWith);
            throw;
        }
    }

    public async Task<bool> UnshareDashboardAsync(int dashboardId, string sharedWith)
    {
        try
        {
            var share = await _context.DashboardShares
                .FirstOrDefaultAsync(s => s.DashboardId == dashboardId && s.SharedWith == sharedWith);

            if (share == null)
            {
                return false;
            }

            _context.DashboardShares.Remove(share);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsharing dashboard {DashboardId} with {SharedWith}", dashboardId, sharedWith);
            throw;
        }
    }

    public async Task<IEnumerable<DashboardShare>> GetDashboardSharesAsync(int dashboardId)
    {
        try
        {
            return await _context.DashboardShares
                .Where(s => s.DashboardId == dashboardId)
                .OrderBy(s => s.SharedWith)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shares for dashboard {DashboardId}", dashboardId);
            throw;
        }
    }

    public async Task<bool> HasAccessAsync(int dashboardId, string username)
    {
        try
        {
            var dashboard = await _context.SharedDashboards.FindAsync(dashboardId);
            if (dashboard == null)
            {
                return false;
            }

            // Owner has access
            if (dashboard.CreatedBy == username)
            {
                return true;
            }

            // Public dashboards are accessible
            if (dashboard.IsPublic)
            {
                return true;
            }

            // Check if shared with user
            var share = await _context.DashboardShares
                .FirstOrDefaultAsync(s => s.DashboardId == dashboardId && s.SharedWith == username);

            return share != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking access for dashboard {DashboardId} and user {Username}", dashboardId, username);
            return false;
        }
    }

    public async Task<bool> CanEditAsync(int dashboardId, string username)
    {
        try
        {
            var dashboard = await _context.SharedDashboards.FindAsync(dashboardId);
            if (dashboard == null)
            {
                return false;
            }

            // Owner can edit
            if (dashboard.CreatedBy == username)
            {
                return true;
            }

            // Check if user has edit permission
            var share = await _context.DashboardShares
                .FirstOrDefaultAsync(s => s.DashboardId == dashboardId && 
                                         s.SharedWith == username && 
                                         (s.Permission == "edit" || s.Permission == "admin"));

            return share != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking edit permission for dashboard {DashboardId} and user {Username}", dashboardId, username);
            return false;
        }
    }

    public async Task RecordAccessAsync(int dashboardId)
    {
        try
        {
            var dashboard = await _context.SharedDashboards.FindAsync(dashboardId);
            if (dashboard != null)
            {
                dashboard.LastAccessedAt = DateTime.UtcNow;
                dashboard.AccessCount++;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording access for dashboard {DashboardId}", dashboardId);
            // Don't throw - this is not critical
        }
    }
}

