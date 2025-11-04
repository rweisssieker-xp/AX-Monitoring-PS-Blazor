using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IAlertService
{
    Task<AlertsResponse?> GetAlertsAsync(string? status = null);
    Task<AlertDto?> CreateAlertAsync(string type, string severity, string message);
    Task<bool> UpdateAlertStatusAsync(int id, string status);
    Task<bool> DeleteAlertAsync(int id);
}

public class AlertService : IAlertService
{
    private readonly IApiService _apiService;

    public AlertService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<AlertsResponse?> GetAlertsAsync(string? status = null)
    {
        var endpoint = string.IsNullOrEmpty(status) 
            ? "api/v1/alerts" 
            : $"api/v1/alerts?status={status}";
        return await _apiService.GetAsync<AlertsResponse>(endpoint);
    }

    public async Task<AlertDto?> CreateAlertAsync(string type, string severity, string message)
    {
        var result = await _apiService.PostAsync<CreateAlertResponse>("api/v1/alerts", new
        {
            type,
            severity,
            message
        });
        return result?.alert;
    }

    public async Task<bool> UpdateAlertStatusAsync(int id, string status)
    {
        var result = await _apiService.PutAsync<object>($"api/v1/alerts/{id}", new { status });
        return result != null;
    }

    public async Task<bool> DeleteAlertAsync(int id)
    {
        return await _apiService.DeleteAsync($"api/v1/alerts/{id}");
    }
}

public class AlertsResponse
{
    public List<AlertDto>? alerts { get; set; }
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class AlertDto
{
    public int Id { get; set; }
    public string AlertId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class CreateAlertResponse
{
    public AlertDto? alert { get; set; }
    public string message { get; set; } = string.Empty;
}

