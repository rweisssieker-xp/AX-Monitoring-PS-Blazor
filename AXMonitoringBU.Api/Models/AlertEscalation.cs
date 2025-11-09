namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Tracks escalation events for alerts
/// </summary>
public class AlertEscalation
{
    public int Id { get; set; }
    public int AlertId { get; set; }
    public int EscalationRuleId { get; set; }
    
    /// <summary>
    /// Escalation level (1 = first, 2 = second, 3 = final)
    /// </summary>
    public int EscalationLevel { get; set; }
    
    /// <summary>
    /// Recipients notified in this escalation
    /// </summary>
    public string Recipients { get; set; } = string.Empty;
    
    /// <summary>
    /// When the escalation was triggered
    /// </summary>
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Minutes since alert creation when escalated
    /// </summary>
    public int MinutesSinceAlert { get; set; }
    
    /// <summary>
    /// Whether escalation was sent via email
    /// </summary>
    public bool SentViaEmail { get; set; }
    
    /// <summary>
    /// Whether escalation was sent via Teams
    /// </summary>
    public bool SentViaTeams { get; set; }
    
    /// <summary>
    /// Error message if escalation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

