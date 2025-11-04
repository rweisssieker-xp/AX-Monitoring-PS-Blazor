namespace AXMonitoringBU.Api.Models;

public class BlockingChain
{
    public int Id { get; set; }
    public string BlockingSessionId { get; set; } = string.Empty;
    public string BlockedSessionId { get; set; } = string.Empty;
    public string BlockingType { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? SqlText { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

