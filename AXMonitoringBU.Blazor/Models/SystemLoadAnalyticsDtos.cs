namespace AXMonitoringBU.Blazor.Models;

public class LoadHeatmapDataDto
{
    public string TimeBucket { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int JobCount { get; set; }
    public int ErrorCount { get; set; }
    public double AvgCpuUsage { get; set; }
    public double AvgMemoryUsage { get; set; }
    public int PeakConcurrentJobs { get; set; }
}

public class LoadHeatmapResponseDto
{
    public List<LoadHeatmapDataDto> HeatmapData { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Granularity { get; set; } = "hourly";
}

public class AosServerLoadDto
{
    public string ServerName { get; set; } = string.Empty;
    public int TotalJobs { get; set; }
    public int RunningJobs { get; set; }
    public int ErrorCount { get; set; }
    public double AvgJobDuration { get; set; }
    public double LoadPercentage { get; set; }
    public string HealthStatus { get; set; } = "Healthy";
}

public class ParallelExecutionDataDto
{
    public DateTime Timestamp { get; set; }
    public int ConcurrentJobs { get; set; }
    public int QueuedJobs { get; set; }
    public double AvgWaitTime { get; set; }
    public double CapacityUtilization { get; set; }
}

public class ResourceTrendDataDto
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double IoWait { get; set; }
    public double TempDbUsage { get; set; }
    public int ActiveConnections { get; set; }
    public int ActiveBatchJobs { get; set; }
}

public class SystemLoadSummaryDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalJobsExecuted { get; set; }
    public int TotalErrors { get; set; }
    public double ErrorRate { get; set; }
    public int PeakConcurrentJobs { get; set; }
    public DateTime PeakLoadTime { get; set; }
    public double AvgCpuUsage { get; set; }
    public double AvgMemoryUsage { get; set; }
    public List<AosServerLoadDto> ServerDistribution { get; set; } = new();
    public string CapacityRecommendation { get; set; } = string.Empty;
}
