using AXMonitoringBU.Blazor.Models;
using System.Globalization;

namespace AXMonitoringBU.Blazor.Services;

public class SystemLoadAnalyticsService : ISystemLoadAnalyticsService
{
    private readonly IApiService _apiService;

    public SystemLoadAnalyticsService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<SystemLoadSummaryDto?> GetSystemLoadSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/load/summary?startDate={startDateStr}&endDate={endDateStr}";
        return await _apiService.GetAsync<SystemLoadSummaryDto>(endpoint);
    }

    public async Task<List<LoadHeatmapDataDto>> GetLoadHeatmapAsync(DateTime startDate, DateTime endDate, string granularity = "hourly")
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/load/heatmap?startDate={startDateStr}&endDate={endDateStr}&granularity={granularity}";
        var response = await _apiService.GetAsync<LoadHeatmapResponseDto>(endpoint);
        return response?.HeatmapData ?? new List<LoadHeatmapDataDto>();
    }

    public async Task<List<AosServerLoadDto>> GetAosServerDistributionAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/load/aos-distribution?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<AosServerDistributionResponse>(endpoint);
        return response?.servers ?? new List<AosServerLoadDto>();
    }

    public async Task<List<ParallelExecutionDataDto>> GetParallelExecutionMetricsAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/load/parallel-execution?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<ParallelExecutionResponse>(endpoint);
        return response?.metrics ?? new List<ParallelExecutionDataDto>();
    }

    public async Task<List<ResourceTrendDataDto>> GetResourceTrendsAsync(DateTime startDate, DateTime endDate)
    {
        var startDateStr = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDateStr = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endpoint = $"api/v1/analytics/load/resource-trends?startDate={startDateStr}&endDate={endDateStr}";
        var response = await _apiService.GetAsync<ResourceTrendsResponse>(endpoint);
        return response?.trends ?? new List<ResourceTrendDataDto>();
    }
}

// Response wrapper classes to match API response format
public class AosServerDistributionResponse
{
    public List<AosServerLoadDto> servers { get; set; } = new();
    public int total_servers { get; set; }
}

public class ParallelExecutionResponse
{
    public List<ParallelExecutionDataDto> metrics { get; set; } = new();
    public int count { get; set; }
}

public class ResourceTrendsResponse
{
    public List<ResourceTrendDataDto> trends { get; set; } = new();
    public int count { get; set; }
}
