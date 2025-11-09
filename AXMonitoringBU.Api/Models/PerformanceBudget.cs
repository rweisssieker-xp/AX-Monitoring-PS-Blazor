namespace AXMonitoringBU.Api.Models;

public class PerformanceBudget
{
    public int Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public double P95ThresholdMs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

