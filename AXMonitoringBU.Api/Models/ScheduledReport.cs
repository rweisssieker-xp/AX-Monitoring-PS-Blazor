namespace AXMonitoringBU.Api.Models;

public class ScheduledReport
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty; // "executive", "detailed"
    public string Schedule { get; set; } = string.Empty; // "daily", "weekly", "monthly"
    public string? CronExpression { get; set; }
    public string Recipients { get; set; } = string.Empty; // Comma-separated emails
    public bool Enabled { get; set; } = true;
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

