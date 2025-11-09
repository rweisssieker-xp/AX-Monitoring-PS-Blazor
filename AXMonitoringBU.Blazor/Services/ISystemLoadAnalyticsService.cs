using AXMonitoringBU.Blazor.Models;

namespace AXMonitoringBU.Blazor.Services;

public interface ISystemLoadAnalyticsService
{
    Task<SystemLoadSummaryDto?> GetSystemLoadSummaryAsync(DateTime startDate, DateTime endDate);
    Task<List<LoadHeatmapDataDto>> GetLoadHeatmapAsync(DateTime startDate, DateTime endDate, string granularity = "hourly");
    Task<List<AosServerLoadDto>> GetAosServerDistributionAsync(DateTime startDate, DateTime endDate);
    Task<List<ParallelExecutionDataDto>> GetParallelExecutionMetricsAsync(DateTime startDate, DateTime endDate);
    Task<List<ResourceTrendDataDto>> GetResourceTrendsAsync(DateTime startDate, DateTime endDate);
}
