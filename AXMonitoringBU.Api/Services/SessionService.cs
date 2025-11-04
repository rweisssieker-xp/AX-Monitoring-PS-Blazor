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
    private readonly ILogger<SessionService> _logger;

    public SessionService(AXDbContext context, ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Session>> GetSessionsAsync(string? status = null)
    {
        try
        {
            var query = _context.Sessions.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            return await query
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
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
            return await _context.Sessions.FindAsync(id);
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
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
            {
                return false;
            }

            // TODO: Implement actual kill logic (call AX API)
            session.Status = "Terminated";
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing session {SessionId}", id);
            throw;
        }
    }
}

