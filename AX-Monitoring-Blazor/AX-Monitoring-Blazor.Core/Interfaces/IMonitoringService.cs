using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IMonitoringService
    {
        Task<KpiDataDto> GetKpiDataAsync();
        Task<List<BatchJobDto>> GetBatchJobsAsync(string status = "All");
        Task<List<SessionDto>> GetSessionsAsync(bool activeOnly = false);
        Task<List<BlockingChainDto>> GetBlockingChainsAsync();
        Task<SqlHealthDto> GetSqlHealthMetricsAsync();
        Task<List<AlertDto>> GetAlertsAsync();
        Task<bool> AcknowledgeAlertAsync(long alertId);
    }
}