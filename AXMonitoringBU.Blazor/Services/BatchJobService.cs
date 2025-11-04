using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IBatchJobService
{
    Task<BatchJobsResponse?> GetBatchJobsAsync(string? status = null);
    Task<bool> RestartBatchJobAsync(int id);
}

public class BatchJobService : IBatchJobService
{
    private readonly IApiService _apiService;

    public BatchJobService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<BatchJobsResponse?> GetBatchJobsAsync(string? status = null)
    {
        var endpoint = string.IsNullOrEmpty(status) 
            ? "api/v1/batch-jobs" 
            : $"api/v1/batch-jobs?status={status}";
        return await _apiService.GetAsync<BatchJobsResponse>(endpoint);
    }

    public async Task<bool> RestartBatchJobAsync(int id)
    {
        return await _apiService.PostAsync<bool>($"api/v1/batch-jobs/{id}/restart", new { });
    }
}

public class BatchJobsResponse
{
    public List<BatchJobDto>? batch_jobs { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class BatchJobDto
{
    public int Id { get; set; }
    public string BatchJobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? EstimatedDuration { get; set; }
    public int Progress { get; set; }
    public string AosServer { get; set; } = string.Empty;
}

