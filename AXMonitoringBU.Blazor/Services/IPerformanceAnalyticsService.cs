using AXMonitoringBU.Blazor.Models;

namespace AXMonitoringBU.Blazor.Services;

public interface IPerformanceAnalyticsService
{
    Task<List<JobDurationTrendDto>> GetJobDurationTrendsAsync(DateTime startDate, DateTime endDate, string? jobCaption = null);
    Task<BaselineComparisonDto?> GetBaselineComparisonAsync(DateTime startDate, DateTime endDate);
    Task<List<SlowestOperationDto>> GetSlowestOperationsAsync(DateTime startDate, DateTime endDate, int topN = 20);
    Task<List<PredictiveWarningDto>> GetPredictiveWarningsAsync();
}
