using System.Net.Http.Json;
using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IBatchJobHistoryService
{
    Task<BatchJobHistoryResponse?> GetBatchJobHistoryAsync(string? captionPattern = null, DateTime? createdFrom = null, int page = 1, int pageSize = 50);
    Task<ErrorAnalysisResponse?> AnalyzeErrorAsync(string caption, DateTime? createdDateTime, string errorReason);
    Task<BatchAnalysisResponse?> AnalyzeErrorsBatchAsync(List<BatchJobHistoryDto> items);
    Task<ErrorTrendsResponse?> GetErrorTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<BatchJobErrorSummaryResponse?> GetErrorSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null);
}

public class BatchJobHistoryService : IBatchJobHistoryService
{
    private readonly IApiService _apiService;

    public BatchJobHistoryService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<BatchJobHistoryResponse?> GetBatchJobHistoryAsync(string? captionPattern = null, DateTime? createdFrom = null, int page = 1, int pageSize = 50)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(captionPattern))
        {
            queryParams.Add($"captionPattern={Uri.EscapeDataString(captionPattern)}");
        }
        if (createdFrom.HasValue)
        {
            queryParams.Add($"createdFrom={createdFrom.Value:yyyy-MM-dd}");
        }
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var endpoint = "api/v1/batch-job-history?" + string.Join("&", queryParams);

        return await _apiService.GetAsync<BatchJobHistoryResponse>(endpoint);
    }

    public async Task<ErrorAnalysisResponse?> AnalyzeErrorAsync(string caption, DateTime? createdDateTime, string errorReason)
    {
        var endpoint = $"api/v1/batch-job-history/analyze";
        var request = new { Caption = caption, CreatedDateTime = createdDateTime, ErrorReason = errorReason };
        
        return await _apiService.PostAsync<ErrorAnalysisResponse>(endpoint, request);
    }

    public async Task<BatchAnalysisResponse?> AnalyzeErrorsBatchAsync(List<BatchJobHistoryDto> items)
    {
        var endpoint = $"api/v1/batch-job-history/analyze-batch";
        var request = new { 
            Items = items
                .Where(i => i.IsError && string.IsNullOrEmpty(i.ErrorCategory))
                .Take(10)
                .Select(i => new { 
                    Caption = i.Caption, 
                    CreatedDateTime = i.CreatedDateTime, 
                    ErrorReason = i.Reason ?? "" 
                })
                .ToList()
        };
        
        return await _apiService.PostAsync<BatchAnalysisResponse>(endpoint, request);
    }

    public async Task<ErrorTrendsResponse?> GetErrorTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var queryParams = new List<string>();
        if (fromDate.HasValue)
        {
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        }
        if (toDate.HasValue)
        {
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        }

        var endpoint = "api/v1/batch-job-history/error-trends";
        if (queryParams.Any())
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        return await _apiService.GetAsync<ErrorTrendsResponse>(endpoint);
    }

    public async Task<BatchJobErrorSummaryResponse?> GetErrorSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var queryParams = new List<string>();
        if (fromDate.HasValue)
        {
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        }
        if (toDate.HasValue)
        {
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
        }

        var endpoint = "api/v1/batch-job-history/error-summary";
        if (queryParams.Any())
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        return await _apiService.GetAsync<BatchJobErrorSummaryResponse>(endpoint);
    }
}

public class BatchJobHistoryResponse
{
    public List<BatchJobHistoryDto>? history { get; set; }
    public int count { get; set; }
    public int? totalCount { get; set; }
    public int? page { get; set; }
    public int? pageSize { get; set; }
    public int? totalPages { get; set; }
    public DateTime timestamp { get; set; }
    public string? error { get; set; }
}

public class BatchJobHistoryDto
{
    public string Caption { get; set; } = string.Empty;
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Reason { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    
    // Error Analysis Fields
    public string? ErrorCategory { get; set; }
    public string? ErrorSeverity { get; set; }
    public string? ErrorAnalysis { get; set; }
    public string? ErrorSuggestions { get; set; }
    public bool IsAnalyzed { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    
    // Helper property
    public bool IsError => !string.IsNullOrEmpty(Reason) && 
                          (Reason.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                           Reason.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                           Reason.Contains("Exception", StringComparison.OrdinalIgnoreCase));
}

public class ErrorAnalysisResponse
{
    public ErrorAnalysisDto? analysis { get; set; }
    public DateTime timestamp { get; set; }
}

public class ErrorAnalysisDto
{
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Explanation { get; set; } = string.Empty;
    public string Suggestions { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
}

public class ErrorTrendsResponse
{
    public List<ErrorTrendDto>? trends { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class ErrorTrendDto
{
    public DateTime Date { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int>? Categories { get; set; }
}

public class BatchJobErrorSummaryResponse
{
    public ErrorStatisticsDto? statistics { get; set; }
    public string? summary { get; set; }
    public DateTime timestamp { get; set; }
}

public class ErrorStatisticsDto
{
    public int TotalJobs { get; set; }
    public int TotalErrors { get; set; }
    public double SuccessRate { get; set; }
    public double ErrorRate { get; set; }
    public List<CategoryCountDto>? TopCategories { get; set; }
    public Dictionary<string, int>? SeverityCounts { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
}

public class CategoryCountDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class BatchAnalysisResponse
{
    public List<object>? results { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

