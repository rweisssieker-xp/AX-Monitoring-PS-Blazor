using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IMaintenanceWindowService
{
    Task<bool> IsInMaintenanceWindowAsync(DateTime? checkTime = null);
    Task<List<MaintenanceWindow>> GetActiveMaintenanceWindowsAsync(DateTime? checkTime = null);
    Task<List<MaintenanceWindow>> GetAllMaintenanceWindowsAsync();
    Task<MaintenanceWindow?> GetMaintenanceWindowByIdAsync(int id);
    Task<MaintenanceWindow> CreateMaintenanceWindowAsync(MaintenanceWindow window);
    Task<bool> UpdateMaintenanceWindowAsync(int id, MaintenanceWindow window);
    Task<bool> DeleteMaintenanceWindowAsync(int id);
}

public class MaintenanceWindowService : IMaintenanceWindowService
{
    private readonly AXDbContext _context;
    private readonly ILogger<MaintenanceWindowService> _logger;
    private readonly IConfiguration _configuration;

    public MaintenanceWindowService(
        AXDbContext context,
        ILogger<MaintenanceWindowService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> IsInMaintenanceWindowAsync(DateTime? checkTime = null)
    {
        try
        {
            var now = checkTime ?? DateTime.UtcNow;
            var activeWindows = await GetActiveMaintenanceWindowsAsync(now);
            return activeWindows.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking maintenance window status");
            return false;
        }
    }

    public async Task<List<MaintenanceWindow>> GetActiveMaintenanceWindowsAsync(DateTime? checkTime = null)
    {
        try
        {
            var now = checkTime ?? DateTime.UtcNow;
            var environment = _configuration["App:Environment"] ?? "DEV";

            var windows = await _context.MaintenanceWindows
                .Where(w => w.Enabled && w.SuppressAlerts)
                .ToListAsync();

            var activeWindows = new List<MaintenanceWindow>();

            foreach (var window in windows)
            {
                // Filter by environment if specified
                if (!string.IsNullOrEmpty(window.Environment) && window.Environment != environment)
                {
                    continue;
                }

                if (window.IsRecurring)
                {
                    if (IsInRecurringWindow(window, now))
                    {
                        activeWindows.Add(window);
                    }
                }
                else
                {
                    // One-time window
                    if (now >= window.StartTime && now <= window.EndTime)
                    {
                        activeWindows.Add(window);
                    }
                }
            }

            return activeWindows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active maintenance windows");
            throw;
        }
    }

    public async Task<List<MaintenanceWindow>> GetAllMaintenanceWindowsAsync()
    {
        try
        {
            return await _context.MaintenanceWindows
                .OrderBy(w => w.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all maintenance windows");
            throw;
        }
    }

    public async Task<MaintenanceWindow?> GetMaintenanceWindowByIdAsync(int id)
    {
        try
        {
            return await _context.MaintenanceWindows.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance window by id {Id}", id);
            throw;
        }
    }

    public async Task<MaintenanceWindow> CreateMaintenanceWindowAsync(MaintenanceWindow window)
    {
        try
        {
            window.CreatedAt = DateTime.UtcNow;
            window.UpdatedAt = DateTime.UtcNow;

            _context.MaintenanceWindows.Add(window);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created maintenance window: {Name} ({StartTime} - {EndTime})", 
                window.Name, window.StartTime, window.EndTime);

            return window;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating maintenance window");
            throw;
        }
    }

    public async Task<bool> UpdateMaintenanceWindowAsync(int id, MaintenanceWindow window)
    {
        try
        {
            var existing = await _context.MaintenanceWindows.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            existing.Name = window.Name;
            existing.Description = window.Description;
            existing.StartTime = window.StartTime;
            existing.EndTime = window.EndTime;
            existing.IsRecurring = window.IsRecurring;
            existing.RecurrencePattern = window.RecurrencePattern;
            existing.DayOfWeek = window.DayOfWeek;
            existing.DayOfMonth = window.DayOfMonth;
            existing.SuppressAlerts = window.SuppressAlerts;
            existing.SuppressNotifications = window.SuppressNotifications;
            existing.Enabled = window.Enabled;
            existing.Environment = window.Environment;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated maintenance window: {Name}", window.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating maintenance window {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteMaintenanceWindowAsync(int id)
    {
        try
        {
            var window = await _context.MaintenanceWindows.FindAsync(id);
            if (window == null)
            {
                return false;
            }

            _context.MaintenanceWindows.Remove(window);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted maintenance window: {Name}", window.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting maintenance window {Id}", id);
            throw;
        }
    }

    private bool IsInRecurringWindow(MaintenanceWindow window, DateTime checkTime)
    {
        if (!window.IsRecurring)
            return false;

        var pattern = window.RecurrencePattern?.ToLower() ?? "";

        switch (pattern)
        {
            case "daily":
                return IsInDailyWindow(window, checkTime);

            case "weekly":
                if (!string.IsNullOrEmpty(window.DayOfWeek))
                {
                    var targetDay = Enum.Parse<DayOfWeek>(window.DayOfWeek, ignoreCase: true);
                    if (checkTime.DayOfWeek != targetDay)
                        return false;
                }
                return IsInDailyWindow(window, checkTime);

            case "monthly":
                if (window.DayOfMonth.HasValue && checkTime.Day != window.DayOfMonth.Value)
                    return false;
                return IsInDailyWindow(window, checkTime);

            default:
                // For now, only support simple patterns
                // Complex cron expressions would require a library like NCrontab
                _logger.LogWarning("Unsupported recurrence pattern: {Pattern}", pattern);
                return false;
        }
    }

    private bool IsInDailyWindow(MaintenanceWindow window, DateTime checkTime)
    {
        var windowStart = window.StartTime.TimeOfDay;
        var windowEnd = window.EndTime.TimeOfDay;
        var checkTimeOfDay = checkTime.TimeOfDay;

        if (windowStart <= windowEnd)
        {
            // Same day window (e.g., 14:00 - 16:00)
            return checkTimeOfDay >= windowStart && checkTimeOfDay <= windowEnd;
        }
        else
        {
            // Overnight window (e.g., 22:00 - 02:00)
            return checkTimeOfDay >= windowStart || checkTimeOfDay <= windowEnd;
        }
    }
}

