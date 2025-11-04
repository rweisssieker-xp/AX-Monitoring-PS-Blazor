using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface ISessionService
{
    Task<IEnumerable<Session>> GetSessionsAsync(string? status = null);
    Task<Session?> GetSessionByIdAsync(int id);
    Task<bool> KillSessionAsync(int id);
}

public class SessionService : ISessionService
{
    private readonly AXDbContext _context;
    private readonly IAXDatabaseService _axDatabaseService;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        AXDbContext context, 
        IAXDatabaseService axDatabaseService,
        ILogger<SessionService> logger)
    {
        _context = context;
        _axDatabaseService = axDatabaseService;
        _logger = logger;
    }

    public async Task<IEnumerable<Session>> GetSessionsAsync(string? status = null)
    {
        try
        {
            // Read directly from AX database
            var axSessions = await _axDatabaseService.GetSessionsFromAXAsync(status);
            
            // Return AX data
            return axSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            throw;
        }
    }

    public async Task<Session?> GetSessionByIdAsync(int id)
    {
        try
        {
            // Try to find in local database first
            var localSession = await _context.Sessions.FindAsync(id);
            if (localSession != null)
            {
                return localSession;
            }

            // If not found, get from AX database
            var axSessions = await _axDatabaseService.GetSessionsFromAXAsync();
            return axSessions.FirstOrDefault(s => s.SessionId == id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session by id {SessionId}", id);
            throw;
        }
    }

    public async Task<bool> KillSessionAsync(int id)
    {
        try
        {
            // First try to get the session
            var session = await GetSessionByIdAsync(id);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", id);
                return false;
            }

            // Try to kill in AX database if SessionId is available
            if (!string.IsNullOrEmpty(session.SessionId))
            {
                var axKilled = await _axDatabaseService.KillSessionInAXAsync(session.SessionId);
                if (axKilled)
                {
                    _logger.LogInformation("Session {SessionId} killed in AX database", id);
                    return true;
                }
            }

            // Fallback: Update local monitoring database
            var localSession = await _context.Sessions.FindAsync(id);
            if (localSession != null)
            {
                localSession.Status = "Terminated";
                localSession.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Session {SessionId} status updated in local database", id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing session {SessionId}", id);
            throw;
        }
    }
}

