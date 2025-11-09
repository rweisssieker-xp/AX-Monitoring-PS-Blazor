using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IAlertService
{
    Task<IEnumerable<Alert>> GetAlertsAsync(string? status = null);
    Task<IEnumerable<Alert>> GetActiveAlertsAsync();
    Task<Alert?> GetAlertByIdAsync(int id);
    Task<Alert> CreateAlertAsync(string type, string severity, string message, string? createdBy = null);
    Task<bool> UpdateAlertStatusAsync(int id, string status);
    Task<bool> DeleteAlertAsync(int id);
    Task<bool> AcknowledgeAlertAsync(int id, string acknowledgedBy);
    Task<Alert?> CheckBaselineAndCreateAlertAsync(string metricName, string metricType, double currentValue, double thresholdPercent = 30.0, string? metricClass = null, string environment = "DEV");
}

public class AlertService : IAlertService
{
    private readonly AXDbContext _context;
    private readonly ILogger<AlertService> _logger;
    private readonly IUserService? _userService;
    private readonly IEmailAlertService? _emailService;
    private readonly ITeamsNotificationService? _teamsService;
    private readonly IBaselineService? _baselineService;
    private readonly IMaintenanceWindowService? _maintenanceWindowService;
    private readonly IWebhookService? _webhookService;

    public AlertService(
        AXDbContext context, 
        ILogger<AlertService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _userService = serviceProvider.GetService<IUserService>();
        _emailService = serviceProvider.GetService<IEmailAlertService>();
        _teamsService = serviceProvider.GetService<ITeamsNotificationService>();
        _baselineService = serviceProvider.GetService<IBaselineService>();
        _maintenanceWindowService = serviceProvider.GetService<IMaintenanceWindowService>();
        _webhookService = serviceProvider.GetService<IWebhookService>();
    }

    public async Task<IEnumerable<Alert>> GetAlertsAsync(string? status = null)
    {
        try
        {
            var query = _context.Alerts.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }

            return await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            throw;
        }
    }

    public async Task<IEnumerable<Alert>> GetActiveAlertsAsync()
    {
        try
        {
            return await _context.Alerts
                .Where(a => a.Status == "Active")
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            throw;
        }
    }

    public async Task<Alert?> GetAlertByIdAsync(int id)
    {
        try
        {
            return await _context.Alerts.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert by id {AlertId}", id);
            throw;
        }
    }

    public async Task<Alert> CreateAlertAsync(string type, string severity, string message, string? createdBy = null)
    {
        try
        {
            // Check if we're in a maintenance window
            if (_maintenanceWindowService != null && await _maintenanceWindowService.IsInMaintenanceWindowAsync())
            {
                var activeWindows = await _maintenanceWindowService.GetActiveMaintenanceWindowsAsync();
                _logger.LogInformation("Skipping alert creation during maintenance window(s): {Windows}", 
                    string.Join(", ", activeWindows.Select(w => w.Name)));
                throw new InvalidOperationException($"Alert creation suppressed during maintenance window: {string.Join(", ", activeWindows.Select(w => w.Name))}");
            }

            // Deduplication: Check if similar alert was created recently (within 15 minutes)
            var dedupeKey = $"{type}:{severity}:{message}";
            var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
            var recentSimilarAlert = await _context.Alerts
                .Where(a => a.Type == type && 
                           a.Severity == severity && 
                           a.Message == message && 
                           a.CreatedAt >= fifteenMinutesAgo &&
                           a.Status == "Active")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (recentSimilarAlert != null)
            {
                _logger.LogInformation("Alert deduplication: Similar alert {AlertId} created {MinutesAgo} minutes ago, skipping duplicate", 
                    recentSimilarAlert.AlertId, 
                    (DateTime.UtcNow - recentSimilarAlert.CreatedAt).TotalMinutes);
                return recentSimilarAlert; // Return existing alert instead of creating duplicate
            }

            // Throttling: Check if too many alerts of this type were created recently (max 1 per 15 minutes per key)
            var recentAlertsCount = await _context.Alerts
                .Where(a => a.Type == type && a.CreatedAt >= fifteenMinutesAgo)
                .CountAsync();

            if (recentAlertsCount >= 1)
            {
                _logger.LogWarning("Alert throttling: Too many alerts of type {Type} in the last 15 minutes ({Count}), suppressing", 
                    type, recentAlertsCount);
                throw new InvalidOperationException($"Alert throttled: Maximum 1 alert per 15 minutes for type '{type}'");
            }

            // Suppression: Check if an alert of this type was created in the last 30 minutes
            // If so, suppress new alerts for 30 minutes after the first one
            var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
            var suppressedAlert = await _context.Alerts
                .Where(a => a.Type == type && 
                           a.Severity == severity && 
                           a.CreatedAt >= thirtyMinutesAgo &&
                           a.Status == "Active")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (suppressedAlert != null)
            {
                var minutesSinceFirstAlert = (DateTime.UtcNow - suppressedAlert.CreatedAt).TotalMinutes;
                if (minutesSinceFirstAlert < 30)
                {
                    _logger.LogInformation("Alert suppression: Alert of type {Type} suppressed for 30 minutes after first alert {AlertId} ({MinutesSince} minutes ago)", 
                        type, suppressedAlert.AlertId, minutesSinceFirstAlert);
                    throw new InvalidOperationException($"Alert suppressed: Similar alert created {minutesSinceFirstAlert:F1} minutes ago. Suppression period: 30 minutes");
                }
            }

            var alert = new Alert
            {
                AlertId = $"ALERT_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                Type = type,
                Severity = severity,
                Message = message,
                Status = "Active",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy ?? (_userService?.GetCurrentWindowsUser() ?? "System")
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            // Send notifications asynchronously (only if not suppressed)
            var suppressNotifications = _maintenanceWindowService != null && 
                await _maintenanceWindowService.IsInMaintenanceWindowAsync();

            if (!suppressNotifications)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_emailService != null)
                        {
                            await _emailService.SendAlertAsync(alert);
                        }
                        if (_teamsService != null)
                        {
                            await _teamsService.SendAlertAsync(alert);
                        }
                        if (_webhookService != null)
                        {
                            // Send webhook to all alert subscriptions
                            var subscriptions = await _webhookService.GetSubscriptionsAsync();
                            var alertSubscriptions = subscriptions.Where(s => 
                                s.Enabled && (s.EventType == "alert" || s.EventType == "all"));
                            
                            foreach (var subscription in alertSubscriptions)
                            {
                                await _webhookService.SendAlertWebhookAsync(subscription.Url, alert);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending notifications for alert {AlertId}", alert.AlertId);
                    }
                });
            }
            else
            {
                _logger.LogInformation("Notifications suppressed for alert {AlertId} during maintenance window", alert.AlertId);
            }

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert");
            throw;
        }
    }

    public async Task<bool> UpdateAlertStatusAsync(int id, string status)
    {
        try
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return false;
            }

            alert.Status = status;
            if (status == "Resolved")
            {
                alert.ResolvedAt = DateTime.UtcNow;
            }
            alert.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert status {AlertId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAlertAsync(int id)
    {
        try
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return false;
            }

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert {AlertId}", id);
            throw;
        }
    }

    public async Task<bool> AcknowledgeAlertAsync(int id, string acknowledgedBy)
    {
        try
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null)
            {
                return false;
            }

            alert.AcknowledgedBy = acknowledgedBy;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.UpdatedAt = DateTime.UtcNow;
            // Don't change status to "Acknowledged" automatically - let user decide
            // If status is "Active", it remains "Active" but is acknowledged

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            throw;
        }
    }

    public async Task<Alert?> CheckBaselineAndCreateAlertAsync(string metricName, string metricType, double currentValue, double thresholdPercent = 30.0, string? metricClass = null, string environment = "DEV")
    {
        try
        {
            if (_baselineService == null)
            {
                _logger.LogWarning("BaselineService not available, skipping baseline check");
                return null;
            }

            var isAboveBaseline = await _baselineService.IsMetricAboveBaselineAsync(
                metricName, metricType, currentValue, thresholdPercent, metricClass, environment);

            if (isAboveBaseline)
            {
                var baseline = await _baselineService.GetBaselineAsync(metricName, metricType, metricClass, environment);
                var threshold = baseline?.Percentile95 * (1 + thresholdPercent / 100.0) ?? currentValue;

                var message = $"Metric {metricName} ({metricType}) exceeds baseline threshold: " +
                    $"Current: {currentValue:F2}, Threshold: {threshold:F2} " +
                    $"(P95={baseline?.Percentile95:F2} + {thresholdPercent}%)";

                if (!string.IsNullOrEmpty(metricClass))
                {
                    message += $", Class: {metricClass}";
                }

                return await CreateAlertAsync(
                    metricType,
                    "Warning",
                    message,
                    "Baseline Monitor");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking baseline and creating alert for {MetricName}/{MetricType}", metricName, metricType);
            return null;
        }
    }
}

