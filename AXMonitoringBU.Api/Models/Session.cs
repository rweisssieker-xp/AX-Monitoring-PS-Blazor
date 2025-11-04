namespace AXMonitoringBU.Api.Models;

public class Session
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AosServer { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivity { get; set; }
    public string Database { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

