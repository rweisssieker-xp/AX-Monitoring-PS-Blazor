namespace AXMonitoringBU.Api.Models;

public class MaintenanceWindow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; } // "Daily", "Weekly", "Monthly", "Yearly", or cron expression
    public string? DayOfWeek { get; set; } // For weekly recurrence: "Monday", "Tuesday", etc.
    public int? DayOfMonth { get; set; } // For monthly recurrence: 1-31
    public bool SuppressAlerts { get; set; } = true;
    public bool SuppressNotifications { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string? Environment { get; set; } // DEV, TST, PRD, or null for all
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

