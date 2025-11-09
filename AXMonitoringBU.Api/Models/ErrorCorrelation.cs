namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Error correlation - tracks which jobs tend to fail together
/// </summary>
public class ErrorCorrelation
{
    public int Id { get; set; }

    /// <summary>
    /// First job name
    /// </summary>
    public string JobNameA { get; set; } = string.Empty;

    /// <summary>
    /// Second job name
    /// </summary>
    public string JobNameB { get; set; } = string.Empty;

    /// <summary>
    /// Correlation coefficient (0-1, 1 = always fail together)
    /// </summary>
    public double CorrelationCoefficient { get; set; }

    /// <summary>
    /// Number of times both jobs failed together
    /// </summary>
    public int CoOccurrenceCount { get; set; }

    /// <summary>
    /// Total number of failures for JobA
    /// </summary>
    public int JobAFailureCount { get; set; }

    /// <summary>
    /// Total number of failures for JobB
    /// </summary>
    public int JobBFailureCount { get; set; }

    /// <summary>
    /// Time window for correlation (minutes) - how close in time do failures need to be
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 30;

    /// <summary>
    /// Is this a dependency (JobA failure causes JobB failure)?
    /// </summary>
    public bool IsDependency { get; set; }

    /// <summary>
    /// Confidence level: Low, Medium, High
    /// </summary>
    public string Confidence { get; set; } = "Medium";

    /// <summary>
    /// When this correlation was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Period start for correlation analysis
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end for correlation analysis
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Root cause analysis data
/// </summary>
public class RootCauseAnalysis
{
    /// <summary>
    /// Error category (from AI analysis)
    /// </summary>
    public string ErrorCategory { get; set; } = string.Empty;

    /// <summary>
    /// Root cause description
    /// </summary>
    public string RootCause { get; set; } = string.Empty;

    /// <summary>
    /// Number of occurrences
    /// </summary>
    public int OccurrenceCount { get; set; }

    /// <summary>
    /// Percentage of total errors
    /// </summary>
    public double PercentageOfTotal { get; set; }

    /// <summary>
    /// Affected job names
    /// </summary>
    public List<string> AffectedJobs { get; set; } = new();

    /// <summary>
    /// First occurrence
    /// </summary>
    public DateTime FirstOccurrence { get; set; }

    /// <summary>
    /// Last occurrence
    /// </summary>
    public DateTime LastOccurrence { get; set; }

    /// <summary>
    /// Trend: Increasing, Stable, Decreasing
    /// </summary>
    public string Trend { get; set; } = "Stable";

    /// <summary>
    /// Recommended action
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// Business impact score (0-10)
    /// </summary>
    public double BusinessImpactScore { get; set; }
}

/// <summary>
/// MTTR (Mean Time To Recovery) metrics
/// </summary>
public class MttrMetric
{
    /// <summary>
    /// Job name or error category
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Type: Job, ErrorCategory, System
    /// </summary>
    public string Type { get; set; } = "Job";

    /// <summary>
    /// Mean time to recovery in minutes
    /// </summary>
    public double MttrMinutes { get; set; }

    /// <summary>
    /// Median time to recovery
    /// </summary>
    public double MedianRecoveryTime { get; set; }

    /// <summary>
    /// 90th percentile recovery time
    /// </summary>
    public double P90RecoveryTime { get; set; }

    /// <summary>
    /// Number of incidents
    /// </summary>
    public int IncidentCount { get; set; }

    /// <summary>
    /// Number of incidents resolved within SLA
    /// </summary>
    public int ResolvedWithinSla { get; set; }

    /// <summary>
    /// SLA compliance rate (%)
    /// </summary>
    public double SlaCompliance { get; set; }

    /// <summary>
    /// Trend compared to previous period (%)
    /// </summary>
    public double TrendPercentage { get; set; }

    /// <summary>
    /// Period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Business impact score for a job or process
/// </summary>
public class BusinessImpact
{
    public int Id { get; set; }

    /// <summary>
    /// Job name or process name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Business process this job belongs to
    /// </summary>
    public string BusinessProcess { get; set; } = string.Empty;

    /// <summary>
    /// Priority: Critical, High, Medium, Low
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Impact score (0-10, 10 = highest impact)
    /// </summary>
    public double ImpactScore { get; set; }

    /// <summary>
    /// Number of business users affected
    /// </summary>
    public int AffectedUsers { get; set; }

    /// <summary>
    /// Financial impact per hour of downtime
    /// </summary>
    public decimal? FinancialImpactPerHour { get; set; }

    /// <summary>
    /// SLA target (minutes)
    /// </summary>
    public int? SlaMinutes { get; set; }

    /// <summary>
    /// Escalation required if failure exceeds this duration
    /// </summary>
    public int? EscalationThresholdMinutes { get; set; }

    /// <summary>
    /// Contact information for escalation
    /// </summary>
    public string? EscalationContact { get; set; }

    /// <summary>
    /// Description of business impact
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Is this job critical for compliance/regulatory purposes?
    /// </summary>
    public bool IsComplianceCritical { get; set; }

    /// <summary>
    /// Created/updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error analytics summary for dashboards
/// </summary>
public class ErrorAnalyticsSummary
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalErrors { get; set; }
    public int UniqueErrorCategories { get; set; }
    public double AvgMttr { get; set; }
    public double SlaCompliance { get; set; }
    public List<RootCauseAnalysis> TopRootCauses { get; set; } = new();
    public List<ErrorCorrelation> StrongestCorrelations { get; set; } = new();
    public List<string> HighImpactJobs { get; set; } = new();
    public string OverallTrend { get; set; } = "Stable";
}
