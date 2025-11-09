namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Extended job execution history with performance metrics
/// </summary>
public class JobExecutionHistory
{
    public int Id { get; set; }

    /// <summary>
    /// Batch Job ID from AX
    /// </summary>
    public string BatchJobId { get; set; } = string.Empty;

    /// <summary>
    /// Job caption/name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// AOS Server that executed the job
    /// </summary>
    public string AosServer { get; set; } = string.Empty;

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Job status: Completed, Error, Running, Cancelled
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// CPU usage during job execution (%)
    /// </summary>
    public double? CpuUsage { get; set; }

    /// <summary>
    /// Memory usage during job execution (MB)
    /// </summary>
    public double? MemoryUsage { get; set; }

    /// <summary>
    /// Number of database queries executed
    /// </summary>
    public int? QueryCount { get; set; }

    /// <summary>
    /// Records processed by the job
    /// </summary>
    public long? RecordsProcessed { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Baseline performance metrics for a specific job
/// </summary>
public class JobBaseline
{
    public int Id { get; set; }

    /// <summary>
    /// Job name/caption pattern
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Baseline duration in seconds (p50)
    /// </summary>
    public double BaselineDurationP50 { get; set; }

    /// <summary>
    /// 90th percentile duration
    /// </summary>
    public double BaselineDurationP90 { get; set; }

    /// <summary>
    /// 95th percentile duration
    /// </summary>
    public double BaselineDurationP95 { get; set; }

    /// <summary>
    /// Expected CPU usage (%)
    /// </summary>
    public double? BaselineCpuUsage { get; set; }

    /// <summary>
    /// Expected memory usage (MB)
    /// </summary>
    public double? BaselineMemoryUsage { get; set; }

    /// <summary>
    /// Expected records processed
    /// </summary>
    public long? BaselineRecordsProcessed { get; set; }

    /// <summary>
    /// Historical error rate (%)
    /// </summary>
    public double HistoricalErrorRate { get; set; }

    /// <summary>
    /// Number of executions used to calculate baseline
    /// </summary>
    public int SampleSize { get; set; }

    /// <summary>
    /// When the baseline was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Period start for baseline calculation
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end for baseline calculation
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Job performance comparison against baseline
/// </summary>
public class JobPerformanceComparison
{
    public string JobName { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public double ActualDuration { get; set; }
    public double BaselineDuration { get; set; }
    public double DurationDeviation { get; set; } // Percentage deviation
    public bool IsSlowerThanBaseline { get; set; }
    public string PerformanceStatus { get; set; } = "Normal"; // Normal, Warning, Critical
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Job duration trend data
/// </summary>
public class JobDurationTrend
{
    public string JobName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double AvgDuration { get; set; }
    public double MinDuration { get; set; }
    public double MaxDuration { get; set; }
    public double P50Duration { get; set; }
    public double P95Duration { get; set; }
    public int ExecutionCount { get; set; }
    public double Trend { get; set; } // Positive = getting slower, negative = getting faster
}
