using AXMonitoringBU.Blazor.Models;
using System.Globalization;

namespace AXMonitoringBU.Blazor.Services;

public class PerformanceAnalyticsService : IPerformanceAnalyticsService
{
    private readonly IApiService _apiService;

    public PerformanceAnalyticsService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<JobDurationTrendDto>> GetJobDurationTrendsAsync(DateTime startDate, DateTime endDate, string? jobCaption = null)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/performance/duration-trends?startDate={startDateStr}&endDate={endDateStr}";

        if (!string.IsNullOrEmpty(jobCaption))
        {
            endpoint += $"&jobCaption={Uri.EscapeDataString(jobCaption)}";
        }

        var response = await _apiService.GetAsync<JobDurationTrendsResponse>(endpoint);
        return response?.trends ?? new List<JobDurationTrendDto>();
    }

    public async Task<BaselineComparisonDto?> GetBaselineComparisonAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/performance/baseline-comparison?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<BaselineComparisonResponse>(endpoint);
        return response?.comparison;
    }

    public async Task<List<SlowestOperationDto>> GetSlowestOperationsAsync(DateTime startDate, DateTime endDate, int topN = 20)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/performance/slowest-operations?startDate={startDateStr}&endDate={endDateStr}&topN={topN}";
        var response = await _apiService.GetAsync<SlowestOperationsResponse>(endpoint);
        return response?.operations ?? new List<SlowestOperationDto>();
    }

    public async Task<List<PredictiveWarningDto>> GetPredictiveWarningsAsync()
    {
        var endpoint = "api/v1/analytics/performance/predictive-warnings";
        var response = await _apiService.GetAsync<PredictiveWarningsResponse>(endpoint);
        return response?.warnings ?? new List<PredictiveWarningDto>();
    }
}
