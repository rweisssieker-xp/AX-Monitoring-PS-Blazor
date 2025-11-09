using Microsoft.JSInterop;
using System.Text.Json;

namespace AXMonitoringBU.Blazor.Services;

public interface ILocalStorageService
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
    Task ClearAsync();
}

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item from localStorage: {Key}", key);
            return default;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting item in localStorage: {Key}", key);
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from localStorage: {Key}", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing localStorage");
        }
    }
}





