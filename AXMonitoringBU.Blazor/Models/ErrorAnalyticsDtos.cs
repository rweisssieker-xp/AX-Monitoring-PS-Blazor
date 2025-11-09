namespace AXMonitoringBU.Blazor.Models;

public class RootCauseAnalysisDto
{
    public string ErrorCategory { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public double Percentage { get; set; }
    public double AvgResolutionTime { get; set; }
    public List<string> AffectedJobs { get; set; } = new();
    public DateTime LastOccurrence { get; set; }
    public string SuggestedRemediation { get; set; } = string.Empty;
}

public class ErrorCorrelationDto
{
    public string PrimaryJob { get; set; } = string.Empty;
    public string CorrelatedJob { get; set; } = string.Empty;
    public double CorrelationStrength { get; set; }
    public int CoOccurrenceCount { get; set; }
    public int TimeWindowMinutes { get; set; }
    public string PotentialRootCause { get; set; } = string.Empty;
}

public class MttrMetricDto
{
    public string JobCaption { get; set; } = string.Empty;
    public int TotalFailures { get; set; }
    public int ResolvedFailures { get; set; }
    public double AvgMttr { get; set; }
    public double MedianMttr { get; set; }
    public double MinMttr { get; set; }
    public double MaxMttr { get; set; }
    public string Status { get; set; } = "Unknown";
    public string Trend { get; set; } = "Stable";
}

public class BusinessImpactDto
{
    public string JobCaption { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int CriticalityScore { get; set; }
    public int AffectedUsers { get; set; }
    public double TotalDowntime { get; set; }
    public decimal EstimatedCost { get; set; }
    public string AffectedProcess { get; set; } = string.Empty;
    public string ImpactLevel { get; set; } = "Medium";
    public DateTime LastOccurrence { get; set; }
    public string RecommendedPriority { get; set; } = "Medium";
}

public class ErrorAnalyticsSummaryDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalErrors { get; set; }
    public int TotalJobsExecuted { get; set; }
    public double OverallErrorRate { get; set; }
    public double AvgMttr { get; set; }
    public string MostCommonErrorCategory { get; set; } = string.Empty;
    public int HighPriorityIssues { get; set; }
    public string Trend { get; set; } = "Stable";
}

// Response wrappers
public class RootCauseResponse
{
    public List<RootCauseAnalysisDto> root_causes { get; set; } = new();
}

public class ErrorCorrelationsResponse
{
    public List<ErrorCorrelationDto> correlations { get; set; } = new();
}

public class MttrResponse
{
    public List<MttrMetricDto> metrics { get; set; } = new();
}

public class BusinessImpactResponse
{
    public List<BusinessImpactDto> impacts { get; set; } = new();
}

public class ErrorSummaryResponse
{
    public ErrorAnalyticsSummaryDto summary { get; set; } = new();
}
