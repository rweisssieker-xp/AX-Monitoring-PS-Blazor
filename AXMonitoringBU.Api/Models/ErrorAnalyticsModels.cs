namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Root cause analysis for errors - DTO
/// </summary>
public class RootCauseAnalysisDto
{
    /// <summary>
    /// Error category or root cause
    /// </summary>
    public string ErrorCategory { get; set; } = string.Empty;

    /// <summary>
    /// Number of occurrences
    /// </summary>
    public int OccurrenceCount { get; set; }

    /// <summary>
    /// Percentage of total errors
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Average time to resolve (minutes)
    /// </summary>
    public double AvgResolutionTime { get; set; }

    /// <summary>
    /// Affected job captions
    /// </summary>
    public List<string> AffectedJobs { get; set; } = new();

    /// <summary>
    /// Most recent occurrence
    /// </summary>
    public DateTime LastOccurrence { get; set; }

    /// <summary>
    /// Suggested remediation action
    /// </summary>
    public string SuggestedRemediation { get; set; } = string.Empty;
}

/// <summary>
/// Error correlation between different job failures - DTO
/// </summary>
public class ErrorCorrelationDto
{
    /// <summary>
    /// Primary job that failed
    /// </summary>
    public string PrimaryJob { get; set; } = string.Empty;

    /// <summary>
    /// Correlated job that also failed
    /// </summary>
    public string CorrelatedJob { get; set; } = string.Empty;

    /// <summary>
    /// Correlation coefficient (0-100)
    /// </summary>
    public double CorrelationStrength { get; set; }

    /// <summary>
    /// Number of times both failed together
    /// </summary>
    public int CoOccurrenceCount { get; set; }

    /// <summary>
    /// Time window for correlation (minutes)
    /// </summary>
    public int TimeWindowMinutes { get; set; }

    /// <summary>
    /// Potential root cause linking the failures
    /// </summary>
    public string PotentialRootCause { get; set; } = string.Empty;
}

/// <summary>
/// Mean Time To Repair (MTTR) metrics - DTO
/// </summary>
public class MttrMetricDto
{
    /// <summary>
    /// Job caption
    /// </summary>
    public string JobCaption { get; set; } = string.Empty;

    /// <summary>
    /// Total number of failures
    /// </summary>
    public int TotalFailures { get; set; }

    /// <summary>
    /// Number of failures resolved
    /// </summary>
    public int ResolvedFailures { get; set; }

    /// <summary>
    /// Average time to repair in minutes
    /// </summary>
    public double AvgMttr { get; set; }

    /// <summary>
    /// Median time to repair in minutes
    /// </summary>
    public double MedianMttr { get; set; }

    /// <summary>
    /// Minimum repair time in minutes
    /// </summary>
    public double MinMttr { get; set; }

    /// <summary>
    /// Maximum repair time in minutes
    /// </summary>
    public double MaxMttr { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Trend: Improving, Stable, Degrading
    /// </summary>
    public string Trend { get; set; } = "Stable";
}

/// <summary>
/// Business impact assessment for errors - DTO
/// </summary>
public class BusinessImpactDto
{
    /// <summary>
    /// Job caption
    /// </summary>
    public string JobCaption { get; set; } = string.Empty;

    /// <summary>
    /// Error count
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Business criticality score (0-100)
    /// </summary>
    public int CriticalityScore { get; set; }

    /// <summary>
    /// Number of affected users/processes
    /// </summary>
    public int AffectedUsers { get; set; }

    /// <summary>
    /// Total downtime in minutes
    /// </summary>
    public double TotalDowntime { get; set; }

    /// <summary>
    /// Estimated financial impact
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Business process affected
    /// </summary>
    public string AffectedProcess { get; set; } = string.Empty;

    /// <summary>
    /// Impact level: Low, Medium, High, Critical
    /// </summary>
    public string ImpactLevel { get; set; } = "Medium";

    /// <summary>
    /// Last occurrence
    /// </summary>
    public DateTime LastOccurrence { get; set; }

    /// <summary>
    /// Recommended priority for fixing
    /// </summary>
    public string RecommendedPriority { get; set; } = "Medium";
}

/// <summary>
/// Error trend over time - DTO
/// </summary>
public class ErrorTrendDto
{
    /// <summary>
    /// Date of the trend data point
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total error count for this period
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Unique jobs that failed
    /// </summary>
    public int UniqueJobsAffected { get; set; }

    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRate { get; set; }
}

/// <summary>
/// Summary of error analytics - DTO
/// </summary>
public class ErrorAnalyticsSummaryDto
{
    /// <summary>
    /// Period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Total errors in period
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// Total jobs executed
    /// </summary>
    public int TotalJobsExecuted { get; set; }

    /// <summary>
    /// Overall error rate
    /// </summary>
    public double OverallErrorRate { get; set; }

    /// <summary>
    /// Average MTTR across all jobs
    /// </summary>
    public double AvgMttr { get; set; }

    /// <summary>
    /// Most common error category
    /// </summary>
    public string MostCommonErrorCategory { get; set; } = string.Empty;

    /// <summary>
    /// High priority issues count
    /// </summary>
    public int HighPriorityIssues { get; set; }

    /// <summary>
    /// Trend: Improving, Stable, Degrading
    /// </summary>
    public string Trend { get; set; } = "Stable";

    /// <summary>
    /// Error trends over time
    /// </summary>
    public List<ErrorTrendDto> ErrorTrends { get; set; } = new();
}
