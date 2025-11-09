namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Represents heatmap data for batch job executions over time
/// </summary>
public class LoadHeatmapData
{
    /// <summary>
    /// Time bucket (hour of day, day of week, etc.)
    /// </summary>
    public string TimeBucket { get; set; } = string.Empty;

    /// <summary>
    /// Date/time of the bucket
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Number of jobs started in this time bucket
    /// </summary>
    public int JobCount { get; set; }

    /// <summary>
    /// Number of job errors in this time bucket
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Average CPU usage during this time bucket (%)
    /// </summary>
    public double AvgCpuUsage { get; set; }

    /// <summary>
    /// Average memory usage during this time bucket (%)
    /// </summary>
    public double AvgMemoryUsage { get; set; }

    /// <summary>
    /// Peak number of concurrent jobs during this time bucket
    /// </summary>
    public int PeakConcurrentJobs { get; set; }
}

/// <summary>
/// Response model for heatmap data
/// </summary>
public class LoadHeatmapResponse
{
    public List<LoadHeatmapData> HeatmapData { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Granularity { get; set; } = "hourly"; // hourly, daily, weekly
}

/// <summary>
/// AOS Server load distribution
/// </summary>
public class AosServerLoad
{
    /// <summary>
    /// AOS Server identifier
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of jobs executed on this server
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Number of running jobs on this server
    /// </summary>
    public int RunningJobs { get; set; }

    /// <summary>
    /// Number of errors on this server
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Average job duration on this server (minutes)
    /// </summary>
    public double AvgJobDuration { get; set; }

    /// <summary>
    /// Load percentage (0-100) - calculated based on job count relative to other servers
    /// </summary>
    public double LoadPercentage { get; set; }

    /// <summary>
    /// Health status: Healthy, Warning, Critical
    /// </summary>
    public string HealthStatus { get; set; } = "Healthy";
}

/// <summary>
/// Parallel execution metrics
/// </summary>
public class ParallelExecutionData
{
    /// <summary>
    /// Timestamp of the measurement
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Number of jobs running in parallel at this time
    /// </summary>
    public int ConcurrentJobs { get; set; }

    /// <summary>
    /// Number of jobs waiting in queue
    /// </summary>
    public int QueuedJobs { get; set; }

    /// <summary>
    /// Average wait time for queued jobs (minutes)
    /// </summary>
    public double AvgWaitTime { get; set; }

    /// <summary>
    /// System capacity utilization (%)
    /// </summary>
    public double CapacityUtilization { get; set; }
}

/// <summary>
/// Resource trend data for capacity planning
/// </summary>
public class ResourceTrendData
{
    /// <summary>
    /// Timestamp of the measurement
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Memory usage percentage
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// Disk I/O wait time (ms)
    /// </summary>
    public double IoWait { get; set; }

    /// <summary>
    /// TempDB usage percentage
    /// </summary>
    public double TempDbUsage { get; set; }

    /// <summary>
    /// Number of active database connections
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Number of batch jobs running at this time
    /// </summary>
    public int ActiveBatchJobs { get; set; }
}

/// <summary>
/// System load analytics summary
/// </summary>
public class SystemLoadSummary
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
    public List<AosServerLoad> ServerDistribution { get; set; } = new();
    public string CapacityRecommendation { get; set; } = string.Empty;
}
