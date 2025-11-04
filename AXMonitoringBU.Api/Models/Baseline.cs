namespace AXMonitoringBU.Api.Models;

public class Baseline
{
    public int Id { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty; // "BatchDuration", "ErrorRate", "Backlog", "Sessions", "Waits", etc.
    public string? MetricClass { get; set; } // For batch jobs: class name
    public string Environment { get; set; } = "DEV"; // DEV, TST, PRD
    public double Percentile50 { get; set; } // P50
    public double Percentile95 { get; set; } // P95
    public double Percentile99 { get; set; } // P99
    public double Mean { get; set; }
    public double StandardDeviation { get; set; }
    public int SampleCount { get; set; }
    public DateTime BaselineDate { get; set; } // Date of baseline calculation
    public DateTime WindowStart { get; set; } // Start of 14-day window
    public DateTime WindowEnd { get; set; } // End of 14-day window
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

