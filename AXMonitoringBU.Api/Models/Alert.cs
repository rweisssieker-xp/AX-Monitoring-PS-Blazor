namespace AXMonitoringBU.Api.Models;

public class Alert
{
    public int Id { get; set; }
    public string AlertId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Correlation ID if this alert is part of a correlated incident
    /// </summary>
    public int? CorrelationId { get; set; }
    
    /// <summary>
    /// Additional metadata for correlation (JSON)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Acknowledged by user
    /// </summary>
    public string? AcknowledgedBy { get; set; }
    
    /// <summary>
    /// When the alert was acknowledged
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
}

