using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IPerformanceAnalyticsService
{
    Task<List<JobDurationTrendDto>> GetJobDurationTrendsAsync(DateTime startDate, DateTime endDate, string? jobCaption = null);
    Task<BaselineComparisonDto> GetBaselineComparisonAsync(DateTime startDate, DateTime endDate);
    Task<List<SlowestOperationDto>> GetSlowestOperationsAsync(DateTime startDate, DateTime endDate, int topN = 20);
    Task<List<PredictiveWarningDto>> GetPredictiveWarningsAsync();
}

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PerformanceAnalyticsService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _connectionString;

    public PerformanceAnalyticsService(
        IConfiguration configuration,
        ILogger<PerformanceAnalyticsService> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    public async Task<List<JobDurationTrendDto>> GetJobDurationTrendsAsync(DateTime startDate, DateTime endDate, string? jobCaption = null)
    {
        var cacheKey = $"job_duration_trends_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{jobCaption ?? "all"}";

        if (_cache.TryGetValue(cacheKey, out List<JobDurationTrendDto>? cached) && cached != null)
        {
            return cached;
        }

        var trends = new List<JobDurationTrendDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var jobFilter = string.IsNullOrEmpty(jobCaption) ? "" : "AND J.CAPTION LIKE @JobCaption";

            var query = $@"
                SELECT
                    J.CAPTION AS JobCaption,
                    CONVERT(DATE, T.STARTDATETIME) AS ExecutionDate,
                    COUNT(*) AS ExecutionCount,
                    AVG(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS AvgDurationSeconds,
                    MIN(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS MinDurationSeconds,
                    MAX(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS MaxDurationSeconds,
                    STDEV(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS StdDevDurationSeconds
                FROM BATCHJOB J WITH (NOLOCK)
                INNER JOIN BATCH T WITH (NOLOCK) ON T.BATCHJOBID = J.RECID
                WHERE T.STARTDATETIME >= @StartDate
                    AND T.STARTDATETIME < @EndDate
                    AND T.ENDDATETIME IS NOT NULL
                    AND T.STARTDATETIME IS NOT NULL
                    AND J.STATUS = 2 -- Finished
                    {jobFilter}
                GROUP BY J.CAPTION, CONVERT(DATE, T.STARTDATETIME)
                ORDER BY J.CAPTION, ExecutionDate";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            if (!string.IsNullOrEmpty(jobCaption))
            {
                command.Parameters.AddWithValue("@JobCaption", $"%{jobCaption}%");
            }

            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                trends.Add(new JobDurationTrendDto
                {
                    JobCaption = reader.GetString(0),
                    ExecutionDate = reader.GetDateTime(1),
                    ExecutionCount = reader.GetInt32(2),
                    AvgDurationSeconds = reader.IsDBNull(3) ? 0 : Convert.ToDouble(reader.GetValue(3)),
                    MinDurationSeconds = reader.IsDBNull(4) ? 0 : Convert.ToDouble(reader.GetValue(4)),
                    MaxDurationSeconds = reader.IsDBNull(5) ? 0 : Convert.ToDouble(reader.GetValue(5)),
                    StdDevDurationSeconds = reader.IsDBNull(6) ? 0 : Convert.ToDouble(reader.GetValue(6))
                });
            }

            _logger.LogInformation("Retrieved {Count} job duration trends", trends.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job duration trends");
            throw;
        }

        // Cache for 10 minutes
        _cache.Set(cacheKey, trends, TimeSpan.FromMinutes(10));

        return trends;
    }

    public async Task<BaselineComparisonDto> GetBaselineComparisonAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"baseline_comparison_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out BaselineComparisonDto? cached) && cached != null)
        {
            return cached;
        }

        var comparison = new BaselineComparisonDto
        {
            PeriodStart = startDate,
            PeriodEnd = endDate
        };

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Calculate baseline from 30 days before the start date
            var baselineStart = startDate.AddDays(-30);
            var baselineEnd = startDate;

            // Get current period stats
            var currentQuery = @"
                SELECT
                    J.CAPTION,
                    COUNT(*) AS ExecutionCount,
                    AVG(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS AvgDuration,
                    SUM(CASE WHEN J.STATUS = 4 THEN 1 ELSE 0 END) AS ErrorCount
                FROM BATCHJOB J WITH (NOLOCK)
                INNER JOIN BATCH T WITH (NOLOCK) ON T.BATCHJOBID = J.RECID
                WHERE T.STARTDATETIME >= @StartDate
                    AND T.STARTDATETIME < @EndDate
                    AND T.ENDDATETIME IS NOT NULL
                    AND T.STARTDATETIME IS NOT NULL
                GROUP BY J.CAPTION
                HAVING COUNT(*) >= 5"; // Only jobs with at least 5 executions

            using var currentCommand = new SqlCommand(currentQuery, connection);
            currentCommand.Parameters.AddWithValue("@StartDate", startDate);
            currentCommand.Parameters.AddWithValue("@EndDate", endDate);
            currentCommand.CommandTimeout = 60;

            var currentStats = new Dictionary<string, (int count, double avgDuration, int errors)>();

            using (var reader = await currentCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var jobCaption = reader.GetString(0);
                    var count = reader.GetInt32(1);
                    var avgDuration = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetValue(2));
                    var errors = reader.GetInt32(3);

                    currentStats[jobCaption] = (count, avgDuration, errors);
                }
            }

            // Get baseline period stats
            var baselineQuery = @"
                SELECT
                    J.CAPTION,
                    COUNT(*) AS ExecutionCount,
                    AVG(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS AvgDuration,
                    SUM(CASE WHEN J.STATUS = 4 THEN 1 ELSE 0 END) AS ErrorCount
                FROM BATCHJOB J WITH (NOLOCK)
                INNER JOIN BATCH T WITH (NOLOCK) ON T.BATCHJOBID = J.RECID
                WHERE T.STARTDATETIME >= @BaselineStart
                    AND T.STARTDATETIME < @BaselineEnd
                    AND T.ENDDATETIME IS NOT NULL
                    AND T.STARTDATETIME IS NOT NULL
                GROUP BY J.CAPTION
                HAVING COUNT(*) >= 5";

            using var baselineCommand = new SqlCommand(baselineQuery, connection);
            baselineCommand.Parameters.AddWithValue("@BaselineStart", baselineStart);
            baselineCommand.Parameters.AddWithValue("@BaselineEnd", baselineEnd);
            baselineCommand.CommandTimeout = 60;

            var baselineStats = new Dictionary<string, (int count, double avgDuration, int errors)>();

            using (var reader = await baselineCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var jobCaption = reader.GetString(0);
                    var count = reader.GetInt32(1);
                    var avgDuration = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetValue(2));
                    var errors = reader.GetInt32(3);

                    baselineStats[jobCaption] = (count, avgDuration, errors);
                }
            }

            // Compare current vs baseline
            comparison.JobComparisons = new List<JobBaselineComparisonDto>();

            foreach (var kvp in currentStats)
            {
                var jobCaption = kvp.Key;
                var current = kvp.Value;

                if (baselineStats.TryGetValue(jobCaption, out var baseline))
                {
                    var durationDiff = baseline.avgDuration > 0
                        ? ((current.avgDuration - baseline.avgDuration) / baseline.avgDuration * 100)
                        : 0;

                    var currentErrorRate = current.count > 0 ? (current.errors * 100.0 / current.count) : 0;
                    var baselineErrorRate = baseline.count > 0 ? (baseline.errors * 100.0 / baseline.count) : 0;

                    comparison.JobComparisons.Add(new JobBaselineComparisonDto
                    {
                        JobCaption = jobCaption,
                        CurrentAvgDuration = current.avgDuration,
                        BaselineAvgDuration = baseline.avgDuration,
                        DurationPercentageChange = Math.Round(durationDiff, 2),
                        CurrentErrorRate = Math.Round(currentErrorRate, 2),
                        BaselineErrorRate = Math.Round(baselineErrorRate, 2),
                        Status = DetermineStatus(durationDiff, currentErrorRate, baselineErrorRate)
                    });
                }
            }

            // Calculate overall metrics
            if (comparison.JobComparisons.Any())
            {
                comparison.OverallDurationChange = Math.Round(
                    comparison.JobComparisons.Average(j => j.DurationPercentageChange), 2);
                comparison.JobsSlowerThanBaseline = comparison.JobComparisons.Count(j => j.DurationPercentageChange > 10);
                comparison.JobsFasterThanBaseline = comparison.JobComparisons.Count(j => j.DurationPercentageChange < -10);
            }

            _logger.LogInformation("Generated baseline comparison with {Count} job comparisons",
                comparison.JobComparisons.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating baseline comparison");
            throw;
        }

        // Cache for 15 minutes
        _cache.Set(cacheKey, comparison, TimeSpan.FromMinutes(15));

        return comparison;
    }

    public async Task<List<SlowestOperationDto>> GetSlowestOperationsAsync(DateTime startDate, DateTime endDate, int topN = 20)
    {
        var cacheKey = $"slowest_operations_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{topN}";

        if (_cache.TryGetValue(cacheKey, out List<SlowestOperationDto>? cached) && cached != null)
        {
            return cached;
        }

        var operations = new List<SlowestOperationDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = $@"
                SELECT TOP {topN}
                    J.CAPTION AS JobCaption,
                    J.RECID AS JobId,
                    T.STARTDATETIME,
                    T.ENDDATETIME,
                    DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME) AS DurationSeconds,
                    ISNULL(T.SERVERID, 'Unknown') AS ServerName,
                    J.STATUS,
                    J.COMPANY
                FROM BATCHJOB J WITH (NOLOCK)
                INNER JOIN BATCH T WITH (NOLOCK) ON T.BATCHJOBID = J.RECID
                WHERE T.STARTDATETIME >= @StartDate
                    AND T.STARTDATETIME < @EndDate
                    AND T.ENDDATETIME IS NOT NULL
                    AND T.STARTDATETIME IS NOT NULL
                    AND DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME) > 0
                ORDER BY DurationSeconds DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                operations.Add(new SlowestOperationDto
                {
                    JobCaption = reader.GetString(0),
                    JobId = reader.GetInt64(1),
                    StartDateTime = reader.GetDateTime(2),
                    EndDateTime = reader.GetDateTime(3),
                    DurationSeconds = Convert.ToDouble(reader.GetValue(4)),
                    ServerName = reader.GetString(5),
                    Status = reader.GetInt32(6),
                    Company = reader.IsDBNull(7) ? "Unknown" : reader.GetString(7)
                });
            }

            _logger.LogInformation("Retrieved {Count} slowest operations", operations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slowest operations");
            throw;
        }

        // Cache for 5 minutes (shorter cache for recent data)
        _cache.Set(cacheKey, operations, TimeSpan.FromMinutes(5));

        return operations;
    }

    public async Task<List<PredictiveWarningDto>> GetPredictiveWarningsAsync()
    {
        var cacheKey = "predictive_warnings";

        if (_cache.TryGetValue(cacheKey, out List<PredictiveWarningDto>? cached) && cached != null)
        {
            return cached;
        }

        var warnings = new List<PredictiveWarningDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Look at last 7 days of data to predict issues
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now;

            // Detect jobs with increasing duration trend
            var trendQuery = @"
                WITH DailyAverages AS (
                    SELECT
                        J.CAPTION,
                        CONVERT(DATE, T.STARTDATETIME) AS ExecutionDate,
                        AVG(DATEDIFF(SECOND, T.STARTDATETIME, T.ENDDATETIME)) AS AvgDuration
                    FROM BATCHJOB J WITH (NOLOCK)
                    INNER JOIN BATCH T WITH (NOLOCK) ON T.BATCHJOBID = J.RECID
                    WHERE T.STARTDATETIME >= @StartDate
                        AND T.STARTDATETIME < @EndDate
                        AND T.ENDDATETIME IS NOT NULL
                        AND T.STARTDATETIME IS NOT NULL
                        AND J.STATUS = 2 -- Finished
                    GROUP BY J.CAPTION, CONVERT(DATE, T.STARTDATETIME)
                    HAVING COUNT(*) >= 3
                ),
                TrendAnalysis AS (
                    SELECT
                        CAPTION,
                        COUNT(*) AS DaysWithData,
                        AVG(AvgDuration) AS OverallAvg,
                        MAX(AvgDuration) AS MaxDuration,
                        MIN(AvgDuration) AS MinDuration
                    FROM DailyAverages
                    GROUP BY CAPTION
                    HAVING COUNT(*) >= 3
                )
                SELECT
                    CAPTION,
                    OverallAvg,
                    MaxDuration,
                    MinDuration,
                    ((MaxDuration - MinDuration) / NULLIF(MinDuration, 0) * 100) AS VariabilityPercent
                FROM TrendAnalysis
                WHERE MaxDuration > MinDuration * 2 -- Duration has doubled
                ORDER BY VariabilityPercent DESC";

            using var command = new SqlCommand(trendQuery, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var caption = reader.GetString(0);
                var avgDuration = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1));
                var variability = reader.IsDBNull(4) ? 0 : Convert.ToDouble(reader.GetValue(4));

                warnings.Add(new PredictiveWarningDto
                {
                    JobCaption = caption,
                    WarningType = "Increasing Duration Trend",
                    Severity = variability > 200 ? "High" : variability > 100 ? "Medium" : "Low",
                    Message = $"Job duration has increased by {Math.Round(variability, 0)}% in the last 7 days. Current average: {Math.Round(avgDuration / 60, 1)} minutes.",
                    PredictedImpactDate = DateTime.Now.AddDays(7),
                    Confidence = variability > 200 ? 85 : variability > 100 ? 70 : 55
                });
            }

            // Detect jobs with increasing error rates
            var errorQuery = @"
                WITH RecentErrors AS (
                    SELECT
                        J.CAPTION,
                        CONVERT(DATE, J.CREATEDDATETIME) AS ErrorDate,
                        COUNT(*) AS ErrorCount
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                        AND J.STATUS = 4 -- Error
                    GROUP BY J.CAPTION, CONVERT(DATE, J.CREATEDDATETIME)
                    HAVING COUNT(*) >= 2
                ),
                ErrorTrend AS (
                    SELECT
                        CAPTION,
                        COUNT(DISTINCT ErrorDate) AS DaysWithErrors,
                        SUM(ErrorCount) AS TotalErrors,
                        AVG(ErrorCount) AS AvgErrorsPerDay
                    FROM RecentErrors
                    GROUP BY CAPTION
                    HAVING COUNT(DISTINCT ErrorDate) >= 3
                )
                SELECT
                    CAPTION,
                    DaysWithErrors,
                    TotalErrors,
                    AvgErrorsPerDay
                FROM ErrorTrend
                WHERE AvgErrorsPerDay >= 2
                ORDER BY TotalErrors DESC";

            using var errorCommand = new SqlCommand(errorQuery, connection);
            errorCommand.Parameters.AddWithValue("@StartDate", startDate);
            errorCommand.Parameters.AddWithValue("@EndDate", endDate);
            errorCommand.CommandTimeout = 60;

            using var errorReader = await errorCommand.ExecuteReaderAsync();

            while (await errorReader.ReadAsync())
            {
                var caption = errorReader.GetString(0);
                var daysWithErrors = errorReader.GetInt32(1);
                var totalErrors = errorReader.GetInt32(2);
                var avgErrorsPerDay = errorReader.IsDBNull(3) ? 0 : Convert.ToDouble(errorReader.GetValue(3));

                var severity = avgErrorsPerDay > 5 ? "High" : avgErrorsPerDay > 3 ? "Medium" : "Low";
                var confidence = daysWithErrors >= 5 ? 90 : daysWithErrors >= 3 ? 75 : 60;

                warnings.Add(new PredictiveWarningDto
                {
                    JobCaption = caption,
                    WarningType = "Increasing Error Rate",
                    Severity = severity,
                    Message = $"Job has failed {totalErrors} times over {daysWithErrors} days (avg {Math.Round(avgErrorsPerDay, 1)} errors/day). Investigate root cause.",
                    PredictedImpactDate = DateTime.Now.AddDays(3),
                    Confidence = confidence
                });
            }

            _logger.LogInformation("Generated {Count} predictive warnings", warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating predictive warnings");
            throw;
        }

        // Cache for 10 minutes
        _cache.Set(cacheKey, warnings, TimeSpan.FromMinutes(10));

        return warnings;
    }

    private string DetermineStatus(double durationChange, double currentErrorRate, double baselineErrorRate)
    {
        if (durationChange > 50 || currentErrorRate > baselineErrorRate + 10)
        {
            return "Critical";
        }
        else if (durationChange > 25 || currentErrorRate > baselineErrorRate + 5)
        {
            return "Warning";
        }
        else if (durationChange < -10 && currentErrorRate <= baselineErrorRate)
        {
            return "Improved";
        }
        else
        {
            return "Normal";
        }
    }
}
