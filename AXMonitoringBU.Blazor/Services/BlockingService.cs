using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IBlockingService
{
    Task<BlockingChainsResponse?> GetBlockingChainsAsync(bool activeOnly = true);
}

public class BlockingService : IBlockingService
{
    private readonly IApiService _apiService;

    public BlockingService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<BlockingChainsResponse?> GetBlockingChainsAsync(bool activeOnly = true)
    {
        var endpoint = $"api/v1/database/blocking?activeOnly={activeOnly}";
        return await _apiService.GetAsync<BlockingChainsResponse>(endpoint);
    }
}

public class BlockingChainsResponse
{
    public List<BlockingChainDto>? blocking_chains { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class BlockingChainDto
{
    public int Id { get; set; }
    public string BlockingSessionId { get; set; } = string.Empty;
    public string BlockedSessionId { get; set; } = string.Empty;
    public string BlockingType { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? SqlText { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

