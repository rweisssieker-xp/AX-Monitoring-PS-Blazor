using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IDeadlockService
{
    Task<DeadlocksResponse?> GetDeadlocksAsync(int count = 100);
    Task<DeadlockDetailResponse?> GetDeadlockByIdAsync(string id);
    Task<DeadlockCountResponse?> GetDeadlockCountAsync(DateTime? since = null);
}

public class DeadlockService : IDeadlockService
{
    private readonly IApiService _apiService;

    public DeadlockService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<DeadlocksResponse?> GetDeadlocksAsync(int count = 100)
    {
        var endpoint = $"api/v1/deadlocks?count={count}";
        return await _apiService.GetAsync<DeadlocksResponse>(endpoint);
    }

    public async Task<DeadlockDetailResponse?> GetDeadlockByIdAsync(string id)
    {
        var endpoint = $"api/v1/deadlocks/{id}";
        return await _apiService.GetAsync<DeadlockDetailResponse>(endpoint);
    }

    public async Task<DeadlockCountResponse?> GetDeadlockCountAsync(DateTime? since = null)
    {
        var endpoint = since.HasValue 
            ? $"api/v1/deadlocks/count?since={since.Value:yyyy-MM-ddTHH:mm:ss}" 
            : "api/v1/deadlocks/count";
        return await _apiService.GetAsync<DeadlockCountResponse>(endpoint);
    }
}

public class DeadlocksResponse
{
    public List<DeadlockDto>? deadlocks { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class DeadlockDetailResponse
{
    public DeadlockDto? deadlock { get; set; }
    public DateTime timestamp { get; set; }
}

public class DeadlockCountResponse
{
    public int count { get; set; }
    public DateTime since { get; set; }
    public DateTime timestamp { get; set; }
}

public class DeadlockDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string VictimSessionId { get; set; } = string.Empty;
    public int process_count { get; set; }
    public int resource_count { get; set; }
    public List<DeadlockProcessDto>? processes { get; set; }
    public List<DeadlockResourceDto>? resources { get; set; }
    public string? DeadlockXml { get; set; }
}

public class DeadlockProcessDto
{
    public string ProcessId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public bool IsVictim { get; set; }
}

public class DeadlockResourceDto
{
    public string ResourceType { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
}

