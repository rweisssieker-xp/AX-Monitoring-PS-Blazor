namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Defines escalation rules for alerts based on time thresholds
/// </summary>
public class AlertEscalationRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Alert type filter (null = all types)
    /// </summary>
    public string? AlertType { get; set; }
    
    /// <summary>
    /// Minimum severity to trigger escalation (Critical, Warning, Info)
    /// </summary>
    public string MinSeverity { get; set; } = "Warning";
    
    /// <summary>
    /// Time threshold in minutes before first escalation
    /// </summary>
    public int FirstEscalationMinutes { get; set; } = 15;
    
    /// <summary>
    /// Recipients for first escalation (comma-separated emails or Teams channels)
    /// </summary>
    public string FirstEscalationRecipients { get; set; } = string.Empty;
    
    /// <summary>
    /// Time threshold in minutes before second escalation
    /// </summary>
    public int? SecondEscalationMinutes { get; set; }
    
    /// <summary>
    /// Recipients for second escalation
    /// </summary>
    public string? SecondEscalationRecipients { get; set; }
    
    /// <summary>
    /// Time threshold in minutes before final escalation (e.g., on-call)
    /// </summary>
    public int? FinalEscalationMinutes { get; set; }
    
    /// <summary>
    /// Recipients for final escalation
    /// </summary>
    public string? FinalEscalationRecipients { get; set; }
    
    /// <summary>
    /// Whether to send escalation via email
    /// </summary>
    public bool EscalateViaEmail { get; set; } = true;
    
    /// <summary>
    /// Whether to send escalation via Teams
    /// </summary>
    public bool EscalateViaTeams { get; set; } = true;
    
    /// <summary>
    /// Whether this rule is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

