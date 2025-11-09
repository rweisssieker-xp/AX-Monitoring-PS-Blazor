namespace AXMonitoringBU.Blazor.Models;

public class JobDurationTrendDto
{
    public string JobCaption { get; set; } = string.Empty;
    public DateTime ExecutionDate { get; set; }
    public int ExecutionCount { get; set; }
    public double AvgDurationSeconds { get; set; }
    public double MinDurationSeconds { get; set; }
    public double MaxDurationSeconds { get; set; }
    public double StdDevDurationSeconds { get; set; }
}

public class BaselineComparisonDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double OverallDurationChange { get; set; }
    public int JobsSlowerThanBaseline { get; set; }
    public int JobsFasterThanBaseline { get; set; }
    public List<JobBaselineComparisonDto> JobComparisons { get; set; } = new();
}

public class JobBaselineComparisonDto
{
    public string JobCaption { get; set; } = string.Empty;
    public double CurrentAvgDuration { get; set; }
    public double BaselineAvgDuration { get; set; }
    public double DurationPercentageChange { get; set; }
    public double CurrentErrorRate { get; set; }
    public double BaselineErrorRate { get; set; }
    public string Status { get; set; } = "Normal";
}

public class SlowestOperationDto
{
    public string JobCaption { get; set; } = string.Empty;
    public long JobId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public double DurationSeconds { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Company { get; set; } = string.Empty;
}

public class PredictiveWarningDto
{
    public string JobCaption { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public string Message { get; set; } = string.Empty;
    public DateTime PredictedImpactDate { get; set; }
    public int Confidence { get; set; }
}

// Response wrapper classes
public class JobDurationTrendsResponse
{
    public List<JobDurationTrendDto> trends { get; set; } = new();
    public int count { get; set; }
}

public class BaselineComparisonResponse
{
    public BaselineComparisonDto comparison { get; set; } = new();
}

public class SlowestOperationsResponse
{
    public List<SlowestOperationDto> operations { get; set; } = new();
    public int actual_count { get; set; }
}

public class PredictiveWarningsResponse
{
    public List<PredictiveWarningDto> warnings { get; set; } = new();
    public int total_warnings { get; set; }
}
