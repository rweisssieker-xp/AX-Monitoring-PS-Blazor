using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IErrorAnalyticsService
{
    Task<List<RootCauseAnalysisDto>> GetRootCauseAnalysisAsync(DateTime startDate, DateTime endDate);
    Task<List<ErrorCorrelationDto>> GetErrorCorrelationsAsync(DateTime startDate, DateTime endDate);
    Task<List<MttrMetricDto>> GetMttrMetricsAsync(DateTime startDate, DateTime endDate);
    Task<List<BusinessImpactDto>> GetBusinessImpactAsync(DateTime startDate, DateTime endDate);
    Task<ErrorAnalyticsSummaryDto> GetErrorSummaryAsync(DateTime startDate, DateTime endDate);
}

public class ErrorAnalyticsService : IErrorAnalyticsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ErrorAnalyticsService> _logger;
    private readonly IMemoryCache _cache;
    private readonly string _connectionString;

    public ErrorAnalyticsService(
        IConfiguration configuration,
        ILogger<ErrorAnalyticsService> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    public async Task<List<RootCauseAnalysisDto>> GetRootCauseAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"root_cause_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out List<RootCauseAnalysisDto>? cached) && cached != null)
        {
            return cached;
        }

        var rootCauses = new List<RootCauseAnalysisDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Analyze errors by caption patterns to identify root causes
            var query = @"
                WITH ErrorCategories AS (
                    SELECT
                        J.CAPTION,
                        J.CREATEDDATETIME AS ErrorTime,
                        CASE
                            WHEN J.CAPTION LIKE '%timeout%' OR J.CAPTION LIKE '%Timeout%' THEN 'Timeout Issues'
                            WHEN J.CAPTION LIKE '%connection%' OR J.CAPTION LIKE '%Connection%' THEN 'Connection Issues'
                            WHEN J.CAPTION LIKE '%permission%' OR J.CAPTION LIKE '%Permission%' OR J.CAPTION LIKE '%access%' THEN 'Permission Issues'
                            WHEN J.CAPTION LIKE '%memory%' OR J.CAPTION LIKE '%Memory%' THEN 'Memory Issues'
                            WHEN J.CAPTION LIKE '%lock%' OR J.CAPTION LIKE '%deadlock%' THEN 'Locking Issues'
                            WHEN J.CAPTION LIKE '%data%' OR J.CAPTION LIKE '%validation%' THEN 'Data Issues'
                            ELSE 'Other Issues'
                        END AS ErrorCategory
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.STATUS = 4 -- Error
                        AND J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                ),
                CategoryStats AS (
                    SELECT
                        ErrorCategory,
                        COUNT(*) AS OccurrenceCount,
                        MAX(ErrorTime) AS LastOccurrence
                    FROM ErrorCategories
                    GROUP BY ErrorCategory
                )
                SELECT
                    ErrorCategory,
                    OccurrenceCount,
                    LastOccurrence,
                    CAST(OccurrenceCount * 100.0 / SUM(OccurrenceCount) OVER () AS FLOAT) AS Percentage
                FROM CategoryStats
                ORDER BY OccurrenceCount DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var category = reader.GetString(0);
                var count = reader.GetInt32(1);
                var lastOccurrence = reader.GetDateTime(2);
                var percentage = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);

                rootCauses.Add(new RootCauseAnalysisDto
                {
                    ErrorCategory = category,
                    OccurrenceCount = count,
                    Percentage = Math.Round(percentage, 2),
                    AvgResolutionTime = 0, // Would need resolution tracking
                    LastOccurrence = lastOccurrence,
                    SuggestedRemediation = GetRemediationSuggestion(category),
                    AffectedJobs = new List<string>() // Will be populated in a second query if needed
                });
            }

            _logger.LogInformation("Retrieved {Count} root cause categories", rootCauses.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving root cause analysis");
            throw;
        }

        // Cache for 15 minutes
        _cache.Set(cacheKey, rootCauses, TimeSpan.FromMinutes(15));

        return rootCauses;
    }

    public async Task<List<ErrorCorrelationDto>> GetErrorCorrelationsAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"error_correlation_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out List<ErrorCorrelationDto>? cached) && cached != null)
        {
            return cached;
        }

        var correlations = new List<ErrorCorrelationDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Find jobs that tend to fail together within a time window
            var query = @"
                WITH FailedJobs AS (
                    SELECT
                        J.CAPTION,
                        J.CREATEDDATETIME AS FailTime,
                        DATEADD(MINUTE, -30, J.CREATEDDATETIME) AS WindowStart,
                        DATEADD(MINUTE, 30, J.CREATEDDATETIME) AS WindowEnd
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.STATUS = 4 -- Error
                        AND J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                ),
                Correlations AS (
                    SELECT
                        F1.CAPTION AS PrimaryJob,
                        F2.CAPTION AS CorrelatedJob,
                        COUNT(*) AS CoOccurrenceCount
                    FROM FailedJobs F1
                    INNER JOIN FailedJobs F2 WITH (NOLOCK)
                        ON F2.FailTime BETWEEN F1.WindowStart AND F1.WindowEnd
                        AND F1.CAPTION < F2.CAPTION -- Avoid duplicates
                    GROUP BY F1.CAPTION, F2.CAPTION
                    HAVING COUNT(*) >= 3 -- At least 3 co-occurrences
                )
                SELECT TOP 20
                    PrimaryJob,
                    CorrelatedJob,
                    CoOccurrenceCount
                FROM Correlations
                ORDER BY CoOccurrenceCount DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var primaryJob = reader.GetString(0);
                var correlatedJob = reader.GetString(1);
                var count = reader.GetInt32(2);

                // Calculate correlation strength (simplified)
                var strength = Math.Min(count * 10.0, 100.0);

                correlations.Add(new ErrorCorrelationDto
                {
                    PrimaryJob = primaryJob,
                    CorrelatedJob = correlatedJob,
                    CorrelationStrength = Math.Round(strength, 1),
                    CoOccurrenceCount = count,
                    TimeWindowMinutes = 60,
                    PotentialRootCause = DetermineRootCause(primaryJob, correlatedJob)
                });
            }

            _logger.LogInformation("Retrieved {Count} error correlations", correlations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error correlations");
            throw;
        }

        // Cache for 15 minutes
        _cache.Set(cacheKey, correlations, TimeSpan.FromMinutes(15));

        return correlations;
    }

    public async Task<List<MttrMetricDto>> GetMttrMetricsAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"mttr_metrics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out List<MttrMetricDto>? cached) && cached != null)
        {
            return cached;
        }

        var metrics = new List<MttrMetricDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Calculate MTTR for jobs (time between failure and next successful run)
            var query = @"
                WITH JobFailures AS (
                    SELECT
                        J.CAPTION,
                        J.CREATEDDATETIME AS FailureTime,
                        J.RECID,
                        ROW_NUMBER() OVER (PARTITION BY J.CAPTION ORDER BY J.CREATEDDATETIME) AS FailureSeq
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.STATUS = 4 -- Error
                        AND J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                ),
                JobSuccesses AS (
                    SELECT
                        J.CAPTION,
                        J.CREATEDDATETIME AS SuccessTime
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.STATUS = 2 -- Finished
                        AND J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                ),
                RepairTimes AS (
                    SELECT
                        F.CAPTION,
                        F.FailureTime,
                        MIN(S.SuccessTime) AS NextSuccessTime,
                        DATEDIFF(MINUTE, F.FailureTime, MIN(S.SuccessTime)) AS RepairTimeMinutes
                    FROM JobFailures F
                    LEFT JOIN JobSuccesses S
                        ON S.CAPTION = F.CAPTION
                        AND S.SuccessTime > F.FailureTime
                    GROUP BY F.CAPTION, F.FailureTime
                ),
                MttrStats AS (
                    SELECT
                        CAPTION,
                        COUNT(*) AS TotalFailures,
                        COUNT(NextSuccessTime) AS ResolvedFailures,
                        AVG(CAST(RepairTimeMinutes AS FLOAT)) AS AvgMttr,
                        MIN(RepairTimeMinutes) AS MinMttr,
                        MAX(RepairTimeMinutes) AS MaxMttr
                    FROM RepairTimes
                    WHERE RepairTimeMinutes IS NOT NULL
                    GROUP BY CAPTION
                    HAVING COUNT(*) >= 2
                )
                SELECT
                    CAPTION,
                    TotalFailures,
                    ResolvedFailures,
                    AvgMttr,
                    MinMttr,
                    MaxMttr
                FROM MttrStats
                ORDER BY AvgMttr DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var caption = reader.GetString(0);
                var totalFailures = reader.GetInt32(1);
                var resolvedFailures = reader.GetInt32(2);
                var avgMttr = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                var minMttr = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
                var maxMttr = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);

                // Determine status and trend
                var resolutionRate = totalFailures > 0 ? (resolvedFailures * 100.0 / totalFailures) : 0;
                var status = resolutionRate > 80 ? "Good" : resolutionRate > 50 ? "Fair" : "Poor";
                var trend = avgMttr < 60 ? "Improving" : avgMttr < 240 ? "Stable" : "Degrading";

                metrics.Add(new MttrMetricDto
                {
                    JobCaption = caption,
                    TotalFailures = totalFailures,
                    ResolvedFailures = resolvedFailures,
                    AvgMttr = Math.Round(avgMttr, 1),
                    MedianMttr = Math.Round(avgMttr, 1), // Simplified - would need percentile calculation
                    MinMttr = Math.Round(minMttr, 1),
                    MaxMttr = Math.Round(maxMttr, 1),
                    Status = status,
                    Trend = trend
                });
            }

            _logger.LogInformation("Retrieved {Count} MTTR metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving MTTR metrics");
            throw;
        }

        // Cache for 10 minutes
        _cache.Set(cacheKey, metrics, TimeSpan.FromMinutes(10));

        return metrics;
    }

    public async Task<List<BusinessImpactDto>> GetBusinessImpactAsync(DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"business_impact_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out List<BusinessImpactDto>? cached) && cached != null)
        {
            return cached;
        }

        var impacts = new List<BusinessImpactDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Calculate business impact based on error frequency and job importance
            var query = @"
                WITH ErrorStats AS (
                    SELECT
                        J.CAPTION,
                        J.COMPANY,
                        COUNT(*) AS ErrorCount,
                        MAX(J.CREATEDDATETIME) AS LastOccurrence
                    FROM BATCHJOB J WITH (NOLOCK)
                    WHERE J.STATUS = 4 -- Error
                        AND J.CREATEDDATETIME >= @StartDate
                        AND J.CREATEDDATETIME < @EndDate
                    GROUP BY J.CAPTION, J.COMPANY
                    HAVING COUNT(*) >= 2
                )
                SELECT
                    CAPTION,
                    COMPANY,
                    ErrorCount,
                    LastOccurrence
                FROM ErrorStats
                ORDER BY ErrorCount DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var caption = reader.GetString(0);
                var company = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                var errorCount = reader.GetInt32(2);
                var lastOccurrence = reader.GetDateTime(3);

                // Calculate criticality score based on various factors
                var criticalityScore = CalculateCriticalityScore(caption, errorCount);
                var affectedUsers = EstimateAffectedUsers(caption, errorCount);
                var downtime = errorCount * 30.0; // Estimate 30 minutes per error
                var estimatedCost = (decimal)(errorCount * 500); // Estimate $500 per error

                var impactLevel = criticalityScore >= 75 ? "Critical" :
                                 criticalityScore >= 50 ? "High" :
                                 criticalityScore >= 25 ? "Medium" : "Low";

                var recommendedPriority = errorCount > 10 ? "Critical" :
                                         errorCount > 5 ? "High" :
                                         errorCount > 2 ? "Medium" : "Low";

                impacts.Add(new BusinessImpactDto
                {
                    JobCaption = caption,
                    ErrorCount = errorCount,
                    CriticalityScore = criticalityScore,
                    AffectedUsers = affectedUsers,
                    TotalDowntime = Math.Round(downtime, 1),
                    EstimatedCost = estimatedCost,
                    AffectedProcess = DetermineAffectedProcess(caption),
                    ImpactLevel = impactLevel,
                    LastOccurrence = lastOccurrence,
                    RecommendedPriority = recommendedPriority
                });
            }

            _logger.LogInformation("Retrieved {Count} business impact assessments", impacts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business impact");
            throw;
        }

        // Cache for 10 minutes
        _cache.Set(cacheKey, impacts, TimeSpan.FromMinutes(10));

        return impacts;
    }

    public async Task<ErrorAnalyticsSummaryDto> GetErrorSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var summary = new ErrorAnalyticsSummaryDto
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
                    COUNT(CASE WHEN J.STATUS = 4 THEN 1 END) AS TotalErrors,
                    COUNT(*) AS TotalJobs
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
                summary.TotalErrors = reader.GetInt32(0);
                summary.TotalJobsExecuted = reader.GetInt32(1);
                summary.OverallErrorRate = summary.TotalJobsExecuted > 0
                    ? Math.Round((summary.TotalErrors * 100.0 / summary.TotalJobsExecuted), 2)
                    : 0;
            }

            // Get root causes to determine most common
            var rootCauses = await GetRootCauseAnalysisAsync(startDate, endDate);
            if (rootCauses.Any())
            {
                summary.MostCommonErrorCategory = rootCauses.First().ErrorCategory;
            }

            // Get business impacts to count high priority issues
            var impacts = await GetBusinessImpactAsync(startDate, endDate);
            summary.HighPriorityIssues = impacts.Count(i => i.RecommendedPriority == "Critical" || i.RecommendedPriority == "High");

            // Determine overall trend
            summary.Trend = summary.OverallErrorRate > 15 ? "Degrading" :
                           summary.OverallErrorRate > 5 ? "Stable" : "Improving";

            _logger.LogInformation("Generated error analytics summary: {Errors} errors, {Rate}% error rate",
                summary.TotalErrors, summary.OverallErrorRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating error summary");
            throw;
        }

        return summary;
    }

    // Helper methods
    private string GetRemediationSuggestion(string category)
    {
        return category switch
        {
            "Timeout Issues" => "Increase timeout values or optimize job execution",
            "Connection Issues" => "Check network connectivity and database availability",
            "Permission Issues" => "Review and update security permissions",
            "Memory Issues" => "Increase memory allocation or optimize resource usage",
            "Locking Issues" => "Review transaction isolation levels and implement retry logic",
            "Data Issues" => "Validate input data and implement data quality checks",
            _ => "Investigate logs and implement error handling"
        };
    }

    private string DetermineRootCause(string primaryJob, string correlatedJob)
    {
        if (primaryJob.Contains("Invoice") && correlatedJob.Contains("Payment"))
        {
            return "Financial Processing Dependency";
        }
        else if (primaryJob.Contains("Import") || correlatedJob.Contains("Import"))
        {
            return "Data Import Dependency";
        }
        else if (primaryJob.Contains("Calculate") || correlatedJob.Contains("Calculate"))
        {
            return "Calculation Dependency";
        }
        return "Shared Resource or Dependency";
    }

    private int CalculateCriticalityScore(string caption, int errorCount)
    {
        var score = errorCount * 5; // Base score on frequency

        // Add weight for critical job types
        if (caption.ToLower().Contains("invoice") || caption.ToLower().Contains("payment"))
            score += 30;
        else if (caption.ToLower().Contains("order") || caption.ToLower().Contains("sales"))
            score += 25;
        else if (caption.ToLower().Contains("inventory") || caption.ToLower().Contains("stock"))
            score += 20;

        return Math.Min(score, 100);
    }

    private int EstimateAffectedUsers(string caption, int errorCount)
    {
        // Estimate based on job type and error frequency
        var baseUsers = errorCount * 2;

        if (caption.ToLower().Contains("invoice") || caption.ToLower().Contains("order"))
            return baseUsers * 5; // Customer-facing
        else if (caption.ToLower().Contains("report"))
            return baseUsers * 3; // Reporting
        else
            return baseUsers;
    }

    private string DetermineAffectedProcess(string caption)
    {
        var lowerCaption = caption.ToLower();

        if (lowerCaption.Contains("invoice") || lowerCaption.Contains("billing"))
            return "Financial Operations";
        else if (lowerCaption.Contains("order") || lowerCaption.Contains("sales"))
            return "Sales Processing";
        else if (lowerCaption.Contains("inventory") || lowerCaption.Contains("stock"))
            return "Inventory Management";
        else if (lowerCaption.Contains("report"))
            return "Reporting & Analytics";
        else if (lowerCaption.Contains("import") || lowerCaption.Contains("export"))
            return "Data Integration";
        else
            return "General Operations";
    }
}
