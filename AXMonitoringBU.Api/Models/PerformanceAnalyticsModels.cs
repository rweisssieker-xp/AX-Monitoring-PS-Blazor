namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Job duration trend over time
/// </summary>
public class JobDurationTrendDto
{
    /// <summary>
    /// Job caption/name
    /// </summary>
    public string JobCaption { get; set; } = string.Empty;

    /// <summary>
    /// Date of execution
    /// </summary>
    public DateTime ExecutionDate { get; set; }

    /// <summary>
    /// Number of times the job was executed on this date
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Average duration in seconds
    /// </summary>
    public double AvgDurationSeconds { get; set; }

    /// <summary>
    /// Minimum duration in seconds
    /// </summary>
    public double MinDurationSeconds { get; set; }

    /// <summary>
    /// Maximum duration in seconds
    /// </summary>
    public double MaxDurationSeconds { get; set; }

    /// <summary>
    /// Standard deviation of duration in seconds
    /// </summary>
    public double StdDevDurationSeconds { get; set; }
}

/// <summary>
/// Comparison of current performance against historical baseline
/// </summary>
public class BaselineComparisonDto
{
    /// <summary>
    /// Start of the current period
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// End of the current period
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Overall percentage change in duration compared to baseline
    /// </summary>
    public double OverallDurationChange { get; set; }

    /// <summary>
    /// Number of jobs running slower than baseline
    /// </summary>
    public int JobsSlowerThanBaseline { get; set; }

    /// <summary>
    /// Number of jobs running faster than baseline
    /// </summary>
    public int JobsFasterThanBaseline { get; set; }

    /// <summary>
    /// Individual job comparisons
    /// </summary>
    public List<JobBaselineComparisonDto> JobComparisons { get; set; } = new();
}

/// <summary>
/// Individual job baseline comparison
/// </summary>
public class JobBaselineComparisonDto
{
    /// <summary>
    /// Job caption/name
    /// </summary>
    public string JobCaption { get; set; } = string.Empty;

    /// <summary>
    /// Current period average duration in seconds
    /// </summary>
    public double CurrentAvgDuration { get; set; }

    /// <summary>
    /// Baseline average duration in seconds
    /// </summary>
    public double BaselineAvgDuration { get; set; }

    /// <summary>
    /// Percentage change in duration
    /// </summary>
    public double DurationPercentageChange { get; set; }

    /// <summary>
    /// Current period error rate (%)
    /// </summary>
    public double CurrentErrorRate { get; set; }

    /// <summary>
    /// Baseline error rate (%)
    /// </summary>
    public double BaselineErrorRate { get; set; }

    /// <summary>
    /// Status: Normal, Warning, Critical, Improved
    /// </summary>
    public string Status { get; set; } = "Normal";
}

/// <summary>
/// Slowest operation/job execution
/// </summary>
public class SlowestOperationDto
{
    /// <summary>
    /// Job caption/name
    /// </summary>
    public string JobCaption { get; set; } = string.Empty;

    /// <summary>
    /// Job record ID
    /// </summary>
    public long JobId { get; set; }

    /// <summary>
    /// Start time of execution
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// End time of execution
    /// </summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Duration in seconds
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// AOS Server where job was executed
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Job status code
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Company/DataAreaId
    /// </summary>
    public string Company { get; set; } = string.Empty;
}

/// <summary>
/// Predictive warning for potential performance issues
/// </summary>
public class PredictiveWarningDto
{
    /// <summary>
    /// Job caption/name
    /// </summary>
    public string JobCaption { get; set; } = string.Empty;

    /// <summary>
    /// Type of warning: Increasing Duration Trend, Increasing Error Rate, etc.
    /// </summary>
    public string WarningType { get; set; } = string.Empty;

    /// <summary>
    /// Severity: Low, Medium, High
    /// </summary>
    public string Severity { get; set; } = "Low";

    /// <summary>
    /// Detailed warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Predicted date when issue might become critical
    /// </summary>
    public DateTime PredictedImpactDate { get; set; }

    /// <summary>
    /// Confidence level (0-100)
    /// </summary>
    public int Confidence { get; set; }
}
