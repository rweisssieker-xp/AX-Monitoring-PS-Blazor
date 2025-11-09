namespace AXMonitoringBU.Api.Models;

public class WebhookSubscription
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "alert", "metric", "all"
    public string? Secret { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}





