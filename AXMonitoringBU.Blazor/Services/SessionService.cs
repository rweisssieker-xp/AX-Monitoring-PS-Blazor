using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface ISessionService
{
    Task<SessionsResponse?> GetSessionsAsync(string? status = null);
    Task<bool> KillSessionAsync(int id);
}

public class SessionService : ISessionService
{
    private readonly IApiService _apiService;

    public SessionService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<SessionsResponse?> GetSessionsAsync(string? status = null)
    {
        var endpoint = string.IsNullOrEmpty(status) 
            ? "api/v1/sessions" 
            : $"api/v1/sessions?status={status}";
        return await _apiService.GetAsync<SessionsResponse>(endpoint);
    }

    public async Task<bool> KillSessionAsync(int id)
    {
        return await _apiService.PostAsync<bool>($"api/v1/sessions/{id}/kill", new { });
    }
}

public class SessionsResponse
{
    public List<SessionDto>? sessions { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class SessionDto
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AosServer { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivity { get; set; }
    public string Database { get; set; } = string.Empty;
}

