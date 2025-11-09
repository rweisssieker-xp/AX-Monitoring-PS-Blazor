using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IAlertEscalationService
{
    Task<IEnumerable<AlertEscalationRule>> GetEscalationRulesAsync(bool? enabled = null);
    Task<AlertEscalationRule?> GetEscalationRuleByIdAsync(int id);
    Task<AlertEscalationRule> CreateEscalationRuleAsync(AlertEscalationRule rule);
    Task<bool> UpdateEscalationRuleAsync(int id, AlertEscalationRule rule);
    Task<bool> DeleteEscalationRuleAsync(int id);
    Task CheckAndEscalateAlertsAsync();
    Task<IEnumerable<AlertEscalation>> GetEscalationsForAlertAsync(int alertId);
}

public class AlertEscalationService : IAlertEscalationService
{
    private readonly AXDbContext _context;
    private readonly ILogger<AlertEscalationService> _logger;
    private readonly IEmailAlertService? _emailService;
    private readonly ITeamsNotificationService? _teamsService;

    public AlertEscalationService(
        AXDbContext context,
        ILogger<AlertEscalationService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _emailService = serviceProvider.GetService<IEmailAlertService>();
        _teamsService = serviceProvider.GetService<ITeamsNotificationService>();
    }

    public async Task<IEnumerable<AlertEscalationRule>> GetEscalationRulesAsync(bool? enabled = null)
    {
        try
        {
            var query = _context.AlertEscalationRules.AsQueryable();
            if (enabled.HasValue)
            {
                query = query.Where(r => r.Enabled == enabled.Value);
            }
            return await query.OrderBy(r => r.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation rules");
            throw;
        }
    }

    public async Task<AlertEscalationRule?> GetEscalationRuleByIdAsync(int id)
    {
        try
        {
            return await _context.AlertEscalationRules.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation rule {RuleId}", id);
            throw;
        }
    }

    public async Task<AlertEscalationRule> CreateEscalationRuleAsync(AlertEscalationRule rule)
    {
        try
        {
            rule.CreatedAt = DateTime.UtcNow;
            _context.AlertEscalationRules.Add(rule);
            await _context.SaveChangesAsync();
            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating escalation rule");
            throw;
        }
    }

    public async Task<bool> UpdateEscalationRuleAsync(int id, AlertEscalationRule rule)
    {
        try
        {
            var existing = await _context.AlertEscalationRules.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            existing.Name = rule.Name;
            existing.Description = rule.Description;
            existing.AlertType = rule.AlertType;
            existing.MinSeverity = rule.MinSeverity;
            existing.FirstEscalationMinutes = rule.FirstEscalationMinutes;
            existing.FirstEscalationRecipients = rule.FirstEscalationRecipients;
            existing.SecondEscalationMinutes = rule.SecondEscalationMinutes;
            existing.SecondEscalationRecipients = rule.SecondEscalationRecipients;
            existing.FinalEscalationMinutes = rule.FinalEscalationMinutes;
            existing.FinalEscalationRecipients = rule.FinalEscalationRecipients;
            existing.EscalateViaEmail = rule.EscalateViaEmail;
            existing.EscalateViaTeams = rule.EscalateViaTeams;
            existing.Enabled = rule.Enabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating escalation rule {RuleId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteEscalationRuleAsync(int id)
    {
        try
        {
            var rule = await _context.AlertEscalationRules.FindAsync(id);
            if (rule == null)
            {
                return false;
            }

            _context.AlertEscalationRules.Remove(rule);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting escalation rule {RuleId}", id);
            throw;
        }
    }

    public async Task CheckAndEscalateAlertsAsync()
    {
        try
        {
            var activeAlerts = await _context.Alerts
                .Where(a => a.Status == "Active")
                .ToListAsync();

            var escalationRules = await _context.AlertEscalationRules
                .Where(r => r.Enabled)
                .ToListAsync();

            foreach (var alert in activeAlerts)
            {
                var minutesSinceAlert = (DateTime.UtcNow - alert.CreatedAt).TotalMinutes;

                foreach (var rule in escalationRules)
                {
                    // Check if rule applies to this alert
                    if (!DoesRuleApply(rule, alert))
                    {
                        continue;
                    }

                    // Check if escalation is needed
                    var escalationLevel = GetEscalationLevel(rule, minutesSinceAlert);
                    if (escalationLevel == 0)
                    {
                        continue;
                    }

                    // Check if this escalation was already sent
                    var existingEscalation = await _context.AlertEscalations
                        .Where(e => e.AlertId == alert.Id && 
                                   e.EscalationRuleId == rule.Id && 
                                   e.EscalationLevel == escalationLevel)
                        .FirstOrDefaultAsync();

                    if (existingEscalation != null)
                    {
                        continue; // Already escalated at this level
                    }

                    // Perform escalation
                    await EscalateAlertAsync(alert, rule, escalationLevel, (int)minutesSinceAlert);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and escalating alerts");
        }
    }

    private bool DoesRuleApply(AlertEscalationRule rule, Alert alert)
    {
        // Check alert type
        if (!string.IsNullOrEmpty(rule.AlertType) && rule.AlertType != alert.Type)
        {
            return false;
        }

        // Check severity
        var severityOrder = new Dictionary<string, int>
        {
            { "Info", 1 },
            { "Warning", 2 },
            { "Critical", 3 }
        };

        var ruleSeverity = severityOrder.GetValueOrDefault(rule.MinSeverity, 0);
        var alertSeverity = severityOrder.GetValueOrDefault(alert.Severity, 0);

        return alertSeverity >= ruleSeverity;
    }

    private int GetEscalationLevel(AlertEscalationRule rule, double minutesSinceAlert)
    {
        if (rule.FinalEscalationMinutes.HasValue && minutesSinceAlert >= rule.FinalEscalationMinutes.Value)
        {
            return 3;
        }
        if (rule.SecondEscalationMinutes.HasValue && minutesSinceAlert >= rule.SecondEscalationMinutes.Value)
        {
            return 2;
        }
        if (minutesSinceAlert >= rule.FirstEscalationMinutes)
        {
            return 1;
        }
        return 0;
    }

    private async Task EscalateAlertAsync(Alert alert, AlertEscalationRule rule, int escalationLevel, int minutesSinceAlert)
    {
        try
        {
            string recipients;
            switch (escalationLevel)
            {
                case 1:
                    recipients = rule.FirstEscalationRecipients;
                    break;
                case 2:
                    recipients = rule.SecondEscalationRecipients ?? string.Empty;
                    break;
                case 3:
                    recipients = rule.FinalEscalationRecipients ?? string.Empty;
                    break;
                default:
                    return;
            }

            if (string.IsNullOrEmpty(recipients))
            {
                return;
            }

            var escalation = new AlertEscalation
            {
                AlertId = alert.Id,
                EscalationRuleId = rule.Id,
                EscalationLevel = escalationLevel,
                Recipients = recipients,
                EscalatedAt = DateTime.UtcNow,
                MinutesSinceAlert = minutesSinceAlert,
                SentViaEmail = false,
                SentViaTeams = false
            };

            var emailSent = false;
            var teamsSent = false;
            var errorMessage = string.Empty;

            try
            {
                if (rule.EscalateViaEmail && _emailService != null)
                {
                    var escalationMessage = $"ALERT ESCALATION (Level {escalationLevel})\n\n" +
                                           $"Alert: {alert.AlertId}\n" +
                                           $"Type: {alert.Type}\n" +
                                           $"Severity: {alert.Severity}\n" +
                                           $"Message: {alert.Message}\n" +
                                           $"Created: {alert.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                           $"Minutes Since Alert: {minutesSinceAlert}\n" +
                                           $"Escalation Rule: {rule.Name}";

                    await _emailService.SendEscalationEmailAsync(recipients, alert, escalationLevel, escalationMessage);
                    emailSent = true;
                }

                if (rule.EscalateViaTeams && _teamsService != null)
                {
                    var teamsMessage = $"🚨 **Alert Escalation (Level {escalationLevel})**\n\n" +
                                      $"**Alert:** {alert.AlertId}\n" +
                                      $"**Type:** {alert.Type}\n" +
                                      $"**Severity:** {alert.Severity}\n" +
                                      $"**Message:** {alert.Message}\n" +
                                      $"**Created:** {alert.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n" +
                                      $"**Minutes Since Alert:** {minutesSinceAlert}\n" +
                                      $"**Escalation Rule:** {rule.Name}";

                    await _teamsService.SendEscalationMessageAsync(recipients, teamsMessage);
                    teamsSent = true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex, "Error sending escalation notifications for alert {AlertId}", alert.AlertId);
            }

            escalation.SentViaEmail = emailSent;
            escalation.SentViaTeams = teamsSent;
            escalation.ErrorMessage = errorMessage;

            _context.AlertEscalations.Add(escalation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Escalated alert {AlertId} to level {Level} via rule {RuleId}", 
                alert.AlertId, escalationLevel, rule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating alert {AlertId}", alert.Id);
        }
    }

    public async Task<IEnumerable<AlertEscalation>> GetEscalationsForAlertAsync(int alertId)
    {
        try
        {
            return await _context.AlertEscalations
                .Where(e => e.AlertId == alertId)
                .OrderBy(e => e.EscalationLevel)
                .ThenBy(e => e.EscalatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalations for alert {AlertId}", alertId);
            throw;
        }
    }
}

