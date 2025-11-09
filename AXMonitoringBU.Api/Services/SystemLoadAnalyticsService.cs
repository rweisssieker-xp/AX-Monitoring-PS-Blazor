using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface ISystemLoadAnalyticsService
{
    Task<LoadHeatmapResponse> GetLoadHeatmapAsync(DateTime startDate, DateTime endDate, string granularity = "hourly");
    Task<List<AosServerLoad>> GetAosServerDistributionAsync(DateTime startDate, DateTime endDate);
    Task<List<ParallelExecutionData>> GetParallelExecutionMetricsAsync(DateTime startDate, DateTime endDate);
    Task<List<ResourceTrendData>> GetResourceTrendsAsync(DateTime startDate, DateTime endDate);
    Task<SystemLoadSummary> GetSystemLoadSummaryAsync(DateTime startDate, DateTime endDate);
}

public class SystemLoadAnalyticsService : ISystemLoadAnalyticsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemLoadAnalyticsService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _connectionString;

    public SystemLoadAnalyticsService(
        IConfiguration configuration,
        ILogger<SystemLoadAnalyticsService> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    public async Task<LoadHeatmapResponse> GetLoadHeatmapAsync(DateTime startDate, DateTime endDate, string granularity = "hourly")
    {
        var cacheKey = $"heatmap_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{granularity}";

        if (_cache.TryGetValue(cacheKey, out LoadHeatmapResponse? cached) && cached != null)
        {
            return cached;
        }

        var heatmapData = new List<LoadHeatmapData>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Group by time bucket based on granularity
            var dateFormat = granularity.ToLower() switch
            {
                "daily" => "CONVERT(DATE, J.CREATEDDATETIME)",
                "weekly" => "DATEPART(YEAR, J.CREATEDDATETIME), DATEPART(WEEK, J.CREATEDDATETIME)",
                _ => "DATEPART(YEAR, J.CREATEDDATETIME), DATEPART(MONTH, J.CREATEDDATETIME), DATEPART(DAY, J.CREATEDDATETIME), DATEPART(HOUR, J.CREATEDDATETIME)"
            };

            var query = $@"
                SELECT
                    {(granularity.ToLower() == "hourly" ? "DATEPART(YEAR, J.CREATEDDATETIME) AS Year, DATEPART(MONTH, J.CREATEDDATETIME) AS Month, DATEPART(DAY, J.CREATEDDATETIME) AS Day, DATEPART(HOUR, J.CREATEDDATETIME) AS Hour," : "")}
                    {(granularity.ToLower() == "daily" ? "CONVERT(DATE, J.CREATEDDATETIME) AS BucketDate," : "")}
                    COUNT(*) AS JobCount,
                    SUM(CASE WHEN J.STATUS = 4 THEN 1 ELSE 0 END) AS ErrorCount
                FROM BATCHJOB J WITH (NOLOCK)
                WHERE J.CREATEDDATETIME >= @StartDate
                    AND J.CREATEDDATETIME < @EndDate
                GROUP BY {dateFormat}
                ORDER BY {dateFormat}";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                DateTime timestamp;
                string timeBucket;

                if (granularity.ToLower() == "hourly")
                {
                    var year = reader.GetInt32(0);
                    var month = reader.GetInt32(1);
                    var day = reader.GetInt32(2);
                    var hour = reader.GetInt32(3);
                    timestamp = new DateTime(year, month, day, hour, 0, 0);
                    timeBucket = timestamp.ToString("yyyy-MM-dd HH:00");
                }
                else if (granularity.ToLower() == "daily")
                {
                    timestamp = reader.GetDateTime(0);
                    timeBucket = timestamp.ToString("yyyy-MM-dd");
                }
                else
                {
                    timestamp = DateTime.Now;
                    timeBucket = "weekly";
                }

                heatmapData.Add(new LoadHeatmapData
                {
                    TimeBucket = timeBucket,
                    Timestamp = timestamp,
                    JobCount = reader.GetInt32(reader.GetOrdinal("JobCount")),
                    ErrorCount = reader.GetInt32(reader.GetOrdinal("ErrorCount")),
                    AvgCpuUsage = 0, // Will be populated from separate query
                    AvgMemoryUsage = 0,
                    PeakConcurrentJobs = 0
                });
            }

            _logger.LogInformation("Retrieved {Count} heatmap data points from {Start} to {End}",
                heatmapData.Count, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving load heatmap data");
            throw;
        }

        var response = new LoadHeatmapResponse
        {
            HeatmapData = heatmapData,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Granularity = granularity
        };

        // Cache for 15 minutes
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));

        return response;
    }

    public async Task<List<AosServerLoad>> GetAosServerDistributionAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"aos_distribution_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out List<AosServerLoad>? cached) && cached != null)
        {
            return cached;
        }

        var serverLoads = new List<AosServerLoad>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT
                    ISNULL(T.SERVERID, 'Unknown') AS ServerName,
                    COUNT(*) AS TotalJobs,
                    SUM(CASE WHEN J.STATUS = 3 THEN 1 ELSE 0 END) AS RunningJobs,
                    SUM(CASE WHEN J.STATUS = 4 THEN 1 ELSE 0 END) AS ErrorCount,
                    AVG(CASE
                        WHEN T.ENDDATETIME IS NOT NULL AND T.STARTDATETIME IS NOT NULL
                        THEN DATEDIFF(MINUTE, T.STARTDATETIME, T.ENDDATETIME)
                        ELSE 0
                    END) AS AvgJobDuration
                FROM BATCHJOB J WITH (NOLOCK)
                LEFT JOIN BATCH T WITH (NOLOCK) ON T.BATCHJOBID = J.RECID
                WHERE J.CREATEDDATETIME >= @StartDate
                    AND J.CREATEDDATETIME < @EndDate
                GROUP BY T.SERVERID
                ORDER BY TotalJobs DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                serverLoads.Add(new AosServerLoad
                {
                    ServerName = reader.GetString(0),
                    TotalJobs = reader.GetInt32(1),
                    RunningJobs = reader.GetInt32(2),
                    ErrorCount = reader.GetInt32(3),
                    AvgJobDuration = reader.IsDBNull(4) ? 0 : reader.GetDouble(4)
                });
            }

            // Calculate load percentage and health status
            if (serverLoads.Any())
            {
                var maxJobs = serverLoads.Max(s => s.TotalJobs);
                foreach (var server in serverLoads)
                {
                    server.LoadPercentage = maxJobs > 0 ? (server.TotalJobs * 100.0 / maxJobs) : 0;

                    var errorRate = server.TotalJobs > 0 ? (server.ErrorCount * 100.0 / server.TotalJobs) : 0;
                    server.HealthStatus = errorRate > 10 ? "Critical" : errorRate > 5 ? "Warning" : "Healthy";
                }
            }

            _logger.LogInformation("Retrieved AOS server distribution for {Count} servers", serverLoads.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AOS server distribution");
            throw;
        }

        // Cache for 10 minutes
        _cache.Set(cacheKey, serverLoads, TimeSpan.FromMinutes(10));

        return serverLoads;
    }

    public async Task<List<ParallelExecutionData>> GetParallelExecutionMetricsAsync(DateTime startDate, DateTime endDate)
    {
        var metrics = new List<ParallelExecutionData>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Sample every hour
            var query = @"
                WITH HourlyBuckets AS (
                    SELECT
                        DATEPART(YEAR, T.STARTDATETIME) AS Year,
                        DATEPART(MONTH, T.STARTDATETIME) AS Month,
                        DATEPART(DAY, T.STARTDATETIME) AS Day,
                        DATEPART(HOUR, T.STARTDATETIME) AS Hour,
                        COUNT(*) AS ConcurrentJobs
                    FROM BATCH T WITH (NOLOCK)
                    INNER JOIN BATCHJOB J WITH (NOLOCK) ON J.RECID = T.BATCHJOBID
                    WHERE T.STARTDATETIME >= @StartDate
                        AND T.STARTDATETIME < @EndDate
                        AND J.STATUS = 3 -- Running
                    GROUP BY
                        DATEPART(YEAR, T.STARTDATETIME),
                        DATEPART(MONTH, T.STARTDATETIME),
                        DATEPART(DAY, T.STARTDATETIME),
                        DATEPART(HOUR, T.STARTDATETIME)
                ),
                QueuedJobs AS (
                    SELECT
                        DATEPART(YEAR, J.CREATEDDATETIME) AS Year,
                        DATEPART(MONTH, J.CREATEDDATETIME) AS Month,
                        DATEPART(DAY, J.CREATEDDATETIME) AS Day,
                        DATEPART(HOUR, J.CREATEDDATETIME) AS Hour,
                        COUNT(*) AS QueueCount
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                        AND J.STATUS = 1 -- Waiting
                    GROUP BY
                        DATEPART(YEAR, J.CREATEDDATETIME),
                        DATEPART(MONTH, J.CREATEDDATETIME),
                        DATEPART(DAY, J.CREATEDDATETIME),
                        DATEPART(HOUR, J.CREATEDDATETIME)
                )
                SELECT
                    COALESCE(H.Year, Q.Year, DATEPART(YEAR, @StartDate)) AS Year,
                    COALESCE(H.Month, Q.Month, DATEPART(MONTH, @StartDate)) AS Month,
                    COALESCE(H.Day, Q.Day, DATEPART(DAY, @StartDate)) AS Day,
                    COALESCE(H.Hour, Q.Hour, 0) AS Hour,
                    ISNULL(H.ConcurrentJobs, 0) AS ConcurrentJobs,
                    ISNULL(Q.QueueCount, 0) AS QueuedJobs
                FROM HourlyBuckets H
                FULL OUTER JOIN QueuedJobs Q
                    ON H.Year = Q.Year
                    AND H.Month = Q.Month
                    AND H.Day = Q.Day
                    AND H.Hour = Q.Hour
                ORDER BY Year, Month, Day, Hour";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var year = reader.GetInt32(0);
                var month = reader.GetInt32(1);
                var day = reader.GetInt32(2);
                var hour = reader.GetInt32(3);
                var timestamp = new DateTime(year, month, day, hour, 0, 0);

                var concurrentJobs = reader.GetInt32(4);
                var queuedJobs = reader.GetInt32(5);
                var totalDemand = concurrentJobs + queuedJobs;

                metrics.Add(new ParallelExecutionData
                {
                    Timestamp = timestamp,
                    ConcurrentJobs = concurrentJobs,
                    QueuedJobs = queuedJobs,
                    AvgWaitTime = queuedJobs > 0 ? (queuedJobs * 5.0) : 0, // Estimate
                    CapacityUtilization = totalDemand > 0 ? (concurrentJobs * 100.0 / totalDemand) : 100
                });
            }

            _logger.LogInformation("Retrieved {Count} parallel execution metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving parallel execution metrics");
            throw;
        }

        return metrics;
    }

    public async Task<List<ResourceTrendData>> GetResourceTrendsAsync(DateTime startDate, DateTime endDate)
    {
        var trends = new List<ResourceTrendData>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Sample every hour - aggregate batch job counts
            var query = @"
                SELECT
                    DATEPART(YEAR, J.CREATEDDATETIME) AS Year,
                    DATEPART(MONTH, J.CREATEDDATETIME) AS Month,
                    DATEPART(DAY, J.CREATEDDATETIME) AS Day,
                    DATEPART(HOUR, J.CREATEDDATETIME) AS Hour,
                    COUNT(*) AS ActiveBatchJobs
                FROM BATCHJOB J WITH (NOLOCK)
                WHERE J.CREATEDDATETIME >= @StartDate
                    AND J.CREATEDDATETIME < @EndDate
                    AND J.STATUS IN (1, 3) -- Waiting or Running
                GROUP BY
                    DATEPART(YEAR, J.CREATEDDATETIME),
                    DATEPART(MONTH, J.CREATEDDATETIME),
                    DATEPART(DAY, J.CREATEDDATETIME),
                    DATEPART(HOUR, J.CREATEDDATETIME)
                ORDER BY Year, Month, Day, Hour";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var year = reader.GetInt32(0);
                var month = reader.GetInt32(1);
                var day = reader.GetInt32(2);
                var hour = reader.GetInt32(3);
                var timestamp = new DateTime(year, month, day, hour, 0, 0);

                trends.Add(new ResourceTrendData
                {
                    Timestamp = timestamp,
                    CpuUsage = 0, // Would need separate monitoring data
                    MemoryUsage = 0, // Would need separate monitoring data
                    IoWait = 0, // Would need separate monitoring data
                    TempDbUsage = 0, // Would need separate monitoring data
                    ActiveConnections = 0, // Would need separate monitoring data
                    ActiveBatchJobs = reader.GetInt32(4)
                });
            }

            _logger.LogInformation("Retrieved {Count} resource trend data points", trends.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource trends");
            throw;
        }

        return trends;
    }

    public async Task<SystemLoadSummary> GetSystemLoadSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var summary = new SystemLoadSummary
        {
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get overall statistics
            var statsQuery = @"
                SELECT
                    COUNT(*) AS TotalJobs,
                    SUM(CASE WHEN J.STATUS = 4 THEN 1 ELSE 0 END) AS TotalErrors
                FROM BATCHJOB J WITH (NOLOCK)
                WHERE J.CREATEDDATETIME >= @StartDate
                    AND J.CREATEDDATETIME < @EndDate";

            using var command = new SqlCommand(statsQuery, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                summary.TotalJobsExecuted = reader.GetInt32(0);
                summary.TotalErrors = reader.GetInt32(1);
                summary.ErrorRate = summary.TotalJobsExecuted > 0
                    ? Math.Round((summary.TotalErrors * 100.0 / summary.TotalJobsExecuted), 2)
                    : 0;
            }

            // Get AOS server distribution
            summary.ServerDistribution = await GetAosServerDistributionAsync(startDate, endDate);

            // Generate capacity recommendation
            if (summary.ErrorRate > 10)
            {
                summary.CapacityRecommendation = "High error rate detected. Review system capacity and job configurations.";
            }
            else if (summary.TotalJobsExecuted > 10000)
            {
                summary.CapacityRecommendation = "High job volume. Consider load balancing across more AOS servers.";
            }
            else
            {
                summary.CapacityRecommendation = "System load is within normal parameters.";
            }

            _logger.LogInformation("Generated system load summary: {TotalJobs} jobs, {ErrorRate}% error rate",
                summary.TotalJobsExecuted, summary.ErrorRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system load summary");
            throw;
        }

        return summary;
    }
}
