using AXMonitoringBU.Blazor.Models;
using System.Globalization;

namespace AXMonitoringBU.Blazor.Services;

public interface IErrorAnalyticsService
{
    Task<List<RootCauseAnalysisDto>> GetRootCauseAnalysisAsync(DateTime startDate, DateTime endDate);
    Task<List<ErrorCorrelationDto>> GetErrorCorrelationsAsync(DateTime startDate, DateTime endDate);
    Task<List<MttrMetricDto>> GetMttrMetricsAsync(DateTime startDate, DateTime endDate);
    Task<List<BusinessImpactDto>> GetBusinessImpactAsync(DateTime startDate, DateTime endDate);
    Task<ErrorAnalyticsSummaryDto?> GetErrorSummaryAsync(DateTime startDate, DateTime endDate);
}

public class ErrorAnalyticsService : IErrorAnalyticsService
{
    private readonly IApiService _apiService;

    public ErrorAnalyticsService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<RootCauseAnalysisDto>> GetRootCauseAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/errors/root-causes?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<RootCauseResponse>(endpoint);
        return response?.root_causes ?? new List<RootCauseAnalysisDto>();
    }

    public async Task<List<ErrorCorrelationDto>> GetErrorCorrelationsAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/errors/correlations?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<ErrorCorrelationsResponse>(endpoint);
        return response?.correlations ?? new List<ErrorCorrelationDto>();
    }

    public async Task<List<MttrMetricDto>> GetMttrMetricsAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/errors/mttr?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<MttrResponse>(endpoint);
        return response?.metrics ?? new List<MttrMetricDto>();
    }

    public async Task<List<BusinessImpactDto>> GetBusinessImpactAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/errors/business-impact?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<BusinessImpactResponse>(endpoint);
        return response?.impacts ?? new List<BusinessImpactDto>();
    }

    public async Task<ErrorAnalyticsSummaryDto?> GetErrorSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/errors/summary?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<ErrorSummaryResponse>(endpoint);
        return response?.summary;
    }
}
