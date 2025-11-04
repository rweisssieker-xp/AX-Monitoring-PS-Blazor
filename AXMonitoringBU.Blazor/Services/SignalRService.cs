using AXMonitoringBU.Blazor.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace AXMonitoringBU.Blazor.Services;

public interface ISignalRService
{
    Task StartAsync();
    Task StopAsync();
    event Action<KpiUpdateData>? OnKpiUpdated;
    event Action<AlertsUpdateData>? OnAlertsUpdated;
    event Action<SystemStatusData>? OnSystemStatusUpdated;
}

public class SignalRService : ISignalRService, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRService> _logger;

    public event Action<KpiUpdateData>? OnKpiUpdated;
    public event Action<AlertsUpdateData>? OnAlertsUpdated;
    public event Action<SystemStatusData>? OnSystemStatusUpdated;

    public SignalRService(HubConnection hubConnection, ILogger<SignalRService> logger)
    {
        _hubConnection = hubConnection;
        _logger = logger;

        try
        {
            // Only register handlers if connection is not disposed
            if (_hubConnection.State != HubConnectionState.Disconnected || !IsDisposed(_hubConnection))
            {
                // Register event handlers
                _hubConnection.On<KpiUpdateData>("KpiUpdated", data =>
                {
                    OnKpiUpdated?.Invoke(data);
                });

                _hubConnection.On<AlertsUpdateData>("AlertsUpdated", data =>
                {
                    OnAlertsUpdated?.Invoke(data);
                });

                _hubConnection.On<SystemStatusData>("SystemStatusUpdated", data =>
                {
                    OnSystemStatusUpdated?.Invoke(data);
                });

                _hubConnection.Closed += async error =>
                {
                    _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
                    await Task.CompletedTask;
                };

                _hubConnection.Reconnecting += async error =>
                {
                    _logger.LogInformation("SignalR reconnecting: {Error}", error?.Message);
                    await Task.CompletedTask;
                };

                _hubConnection.Reconnected += async connectionId =>
                {
                    _logger.LogInformation("SignalR reconnected: {ConnectionId}", connectionId);
                    await Task.CompletedTask;
                };
            }
        }
        catch (ObjectDisposedException)
        {
            _logger.LogWarning("HubConnection is already disposed, skipping handler registration");
        }
    }

    private static bool IsDisposed(HubConnection connection)
    {
        try
        {
            _ = connection.State;
            return false;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connection started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting SignalR connection");
                throw;
            }
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
            _logger.LogInformation("SignalR connection stopped");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _hubConnection.DisposeAsync();
    }
}

public class KpiUpdateData
{
    public Dictionary<string, object>? kpis { get; set; }
    public Dictionary<string, object>? sql_health { get; set; }
    public DateTime timestamp { get; set; }
}

public class AlertsUpdateData
{
    public int active_count { get; set; }
    public int critical_count { get; set; }
    public List<AlertDto>? alerts { get; set; }
    public DateTime timestamp { get; set; }
}

public class SystemStatusData
{
    public string status { get; set; } = string.Empty;
    public DateTime timestamp { get; set; }
}

