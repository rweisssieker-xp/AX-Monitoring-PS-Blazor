using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IBatchService
    {
        Task<List<BatchJobDto>> GetBatchJobsAsync(string status = "All");
        Task<Dictionary<string, object>> GetBatchJobStatisticsAsync(int lastHours = 24);
    }
}