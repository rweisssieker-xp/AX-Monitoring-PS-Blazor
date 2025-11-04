namespace AXMonitoringBU.Api.Models;

public class BatchJob
{
    public int Id { get; set; }
    public string BatchJobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? EstimatedDuration { get; set; }
    public int Progress { get; set; }
    public string AosServer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

