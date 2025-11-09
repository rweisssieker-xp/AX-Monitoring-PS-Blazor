namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Represents a group of correlated alerts that form an incident
/// </summary>
public class AlertCorrelation
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier for this correlation group
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title/Summary of the incident
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the correlation
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Severity of the incident (highest severity from correlated alerts)
    /// </summary>
    public string Severity { get; set; } = string.Empty;
    
    /// <summary>
    /// Status of the incident (Open, Investigating, Resolved, Closed)
    /// </summary>
    public string Status { get; set; } = "Open";
    
    /// <summary>
    /// When the incident was first detected
    /// </summary>
    public DateTime FirstDetectedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the incident was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
    
    /// <summary>
    /// Number of alerts in this correlation
    /// </summary>
    public int AlertCount { get; set; }
    
    /// <summary>
    /// Correlation confidence score (0-100)
    /// </summary>
    public int ConfidenceScore { get; set; }
    
    /// <summary>
    /// Correlation reason (e.g., "Same AOS Server", "Same Time Window", "Related Metrics")
    /// </summary>
    public string CorrelationReason { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

