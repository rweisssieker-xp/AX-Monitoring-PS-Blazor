using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;
using System.Text.Json;

namespace AXMonitoringBU.Api.Services;

public interface IAlertCorrelationService
{
    Task<IEnumerable<AlertCorrelation>> GetCorrelationsAsync(string? status = null);
    Task<AlertCorrelation?> GetCorrelationByIdAsync(int id);
    Task<AlertCorrelation?> CorrelateAlertsAsync();
    Task<bool> ResolveCorrelationAsync(int correlationId);
    Task<IEnumerable<Alert>> GetAlertsForCorrelationAsync(int correlationId);
}

public class AlertCorrelationService : IAlertCorrelationService
{
    private readonly AXDbContext _context;
    private readonly ILogger<AlertCorrelationService> _logger;

    public AlertCorrelationService(
        AXDbContext context,
        ILogger<AlertCorrelationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AlertCorrelation>> GetCorrelationsAsync(string? status = null)
    {
        try
        {
            var query = _context.AlertCorrelations.AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }
            return await query
                .OrderByDescending(c => c.FirstDetectedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correlations");
            throw;
        }
    }

    public async Task<AlertCorrelation?> GetCorrelationByIdAsync(int id)
    {
        try
        {
            return await _context.AlertCorrelations.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correlation {CorrelationId}", id);
            throw;
        }
    }

    public async Task<AlertCorrelation?> CorrelateAlertsAsync()
    {
        try
        {
            // Get active alerts from the last hour
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var activeAlerts = await _context.Alerts
                .Where(a => a.Status == "Active" && a.CreatedAt >= oneHourAgo && a.CorrelationId == null)
                .ToListAsync();

            if (activeAlerts.Count < 2)
            {
                return null; // Need at least 2 alerts to correlate
            }

            // Group alerts by correlation criteria
            var correlatedGroups = new List<List<Alert>>();

            // Group 1: Same type and same time window (within 5 minutes)
            var sameTypeGroups = activeAlerts
                .GroupBy(a => a.Type)
                .Where(g => g.Count() >= 2)
                .ToList();

            foreach (var group in sameTypeGroups)
            {
                var alerts = group.OrderBy(a => a.CreatedAt).ToList();
                var correlated = new List<Alert> { alerts[0] };

                for (int i = 1; i < alerts.Count; i++)
                {
                    var timeDiff = (alerts[i].CreatedAt - alerts[0].CreatedAt).TotalMinutes;
                    if (timeDiff <= 5)
                    {
                        correlated.Add(alerts[i]);
                    }
                    else
                    {
                        if (correlated.Count >= 2)
                        {
                            correlatedGroups.Add(correlated);
                        }
                        correlated = new List<Alert> { alerts[i] };
                    }
                }

                if (correlated.Count >= 2)
                {
                    correlatedGroups.Add(correlated);
                }
            }

            // Group 2: Same severity and same AOS server (if metadata contains server info)
            var sameSeverityGroups = activeAlerts
                .Where(a => !string.IsNullOrEmpty(a.Metadata))
                .GroupBy(a => new { a.Severity, Server = ExtractServerFromMetadata(a.Metadata) })
                .Where(g => g.Count() >= 2 && !string.IsNullOrEmpty(g.Key.Server))
                .ToList();

            foreach (var group in sameSeverityGroups)
            {
                var alerts = group.OrderBy(a => a.CreatedAt).ToList();
                var timeWindow = 10; // 10 minutes window
                var correlated = new List<Alert> { alerts[0] };

                for (int i = 1; i < alerts.Count; i++)
                {
                    var timeDiff = (alerts[i].CreatedAt - alerts[0].CreatedAt).TotalMinutes;
                    if (timeDiff <= timeWindow)
                    {
                        correlated.Add(alerts[i]);
                    }
                }

                if (correlated.Count >= 2)
                {
                    correlatedGroups.Add(correlated);
                }
            }

            // Create correlation for the largest group
            if (!correlatedGroups.Any())
            {
                return null;
            }

            var largestGroup = correlatedGroups.OrderByDescending(g => g.Count).First();
            if (largestGroup.Count < 2)
            {
                return null;
            }

            // Check if correlation already exists for these alerts
            var existingCorrelationIds = largestGroup
                .Where(a => a.CorrelationId.HasValue)
                .Select(a => a.CorrelationId!.Value)
                .Distinct()
                .ToList();

            if (existingCorrelationIds.Any())
            {
                // Add to existing correlation
                var existingCorrelation = await _context.AlertCorrelations
                    .FirstOrDefaultAsync(c => existingCorrelationIds.Contains(c.Id));

                if (existingCorrelation != null)
                {
                    foreach (var alert in largestGroup.Where(a => !a.CorrelationId.HasValue))
                    {
                        alert.CorrelationId = existingCorrelation.Id;
                    }

                    existingCorrelation.AlertCount = await _context.Alerts
                        .CountAsync(a => a.CorrelationId == existingCorrelation.Id);
                    existingCorrelation.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return existingCorrelation;
                }
            }

            // Create new correlation
            var correlation = new AlertCorrelation
            {
                CorrelationId = $"CORR_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}",
                Title = $"Incident: {largestGroup[0].Type} ({largestGroup.Count} alerts)",
                Description = $"Correlated {largestGroup.Count} alerts of type '{largestGroup[0].Type}' detected within a short time window.",
                Severity = GetHighestSeverity(largestGroup),
                Status = "Open",
                FirstDetectedAt = largestGroup.Min(a => a.CreatedAt),
                AlertCount = largestGroup.Count,
                ConfidenceScore = CalculateConfidenceScore(largestGroup),
                CorrelationReason = DetermineCorrelationReason(largestGroup),
                CreatedAt = DateTime.UtcNow
            };

            _context.AlertCorrelations.Add(correlation);
            await _context.SaveChangesAsync();

            // Update alerts with correlation ID
            foreach (var alert in largestGroup)
            {
                alert.CorrelationId = correlation.Id;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created correlation {CorrelationId} for {Count} alerts", 
                correlation.CorrelationId, largestGroup.Count);

            return correlation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error correlating alerts");
            throw;
        }
    }

    public async Task<bool> ResolveCorrelationAsync(int correlationId)
    {
        try
        {
            var correlation = await _context.AlertCorrelations.FindAsync(correlationId);
            if (correlation == null)
            {
                return false;
            }

            correlation.Status = "Resolved";
            correlation.ResolvedAt = DateTime.UtcNow;
            correlation.UpdatedAt = DateTime.UtcNow;

            // Also resolve all alerts in this correlation
            var alerts = await _context.Alerts
                .Where(a => a.CorrelationId == correlationId && a.Status == "Active")
                .ToListAsync();

            foreach (var alert in alerts)
            {
                alert.Status = "Resolved";
                alert.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving correlation {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<IEnumerable<Alert>> GetAlertsForCorrelationAsync(int correlationId)
    {
        try
        {
            return await _context.Alerts
                .Where(a => a.CorrelationId == correlationId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts for correlation {CorrelationId}", correlationId);
            throw;
        }
    }

    private string ExtractServerFromMetadata(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
        {
            return string.Empty;
        }

        try
        {
            var json = JsonDocument.Parse(metadata);
            if (json.RootElement.TryGetProperty("AosServer", out var server))
            {
                return server.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return string.Empty;
    }

    private string GetHighestSeverity(List<Alert> alerts)
    {
        var severityOrder = new Dictionary<string, int>
        {
            { "Info", 1 },
            { "Warning", 2 },
            { "Critical", 3 }
        };

        return alerts
            .OrderByDescending(a => severityOrder.GetValueOrDefault(a.Severity, 0))
            .First()
            .Severity;
    }

    private int CalculateConfidenceScore(List<Alert> alerts)
    {
        // Base score
        var score = 50;

        // Same type adds confidence
        if (alerts.All(a => a.Type == alerts[0].Type))
        {
            score += 20;
        }

        // Time proximity adds confidence
        var timeSpan = alerts.Max(a => a.CreatedAt) - alerts.Min(a => a.CreatedAt);
        if (timeSpan.TotalMinutes <= 5)
        {
            score += 20;
        }
        else if (timeSpan.TotalMinutes <= 15)
        {
            score += 10;
        }

        // Same severity adds confidence
        if (alerts.All(a => a.Severity == alerts[0].Severity))
        {
            score += 10;
        }

        return Math.Min(100, score);
    }

    private string DetermineCorrelationReason(List<Alert> alerts)
    {
        if (alerts.All(a => a.Type == alerts[0].Type))
        {
            var timeSpan = alerts.Max(a => a.CreatedAt) - alerts.Min(a => a.CreatedAt);
            if (timeSpan.TotalMinutes <= 5)
            {
                return $"Same Type ({alerts[0].Type}) within 5 minutes";
            }
            return $"Same Type ({alerts[0].Type}) within time window";
        }

        var servers = alerts
            .Select(a => ExtractServerFromMetadata(a.Metadata))
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList();

        if (servers.Count == 1)
        {
            return $"Same AOS Server ({servers[0]})";
        }

        return "Multiple related alerts detected";
    }
}

