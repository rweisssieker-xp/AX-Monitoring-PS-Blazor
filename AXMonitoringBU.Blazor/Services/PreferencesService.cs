using Microsoft.JSInterop;

namespace AXMonitoringBU.Blazor.Services;

public interface IPreferencesService
{
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string theme);
    Task<int> GetRefreshIntervalAsync();
    Task SetRefreshIntervalAsync(int seconds);
    Task<Dictionary<string, string>> GetPreferencesAsync();
    Task SetPreferenceAsync(string key, string value);
}

public class PreferencesService : IPreferencesService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(
        IJSRuntime jsRuntime,
        ILogger<PreferencesService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<string> GetThemeAsync()
    {
        try
        {
            var theme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
            return theme ?? "light";
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering
            return "light";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theme preference");
            return "light";
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", theme);
            await _jsRuntime.InvokeVoidAsync("applyTheme", theme);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting theme preference");
        }
    }

    public async Task<int> GetRefreshIntervalAsync()
    {
        try
        {
            var interval = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "refreshInterval");
            return int.TryParse(interval, out var result) ? result : 30;
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering
            return 30;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refresh interval preference");
            return 30;
        }
    }

    public async Task SetRefreshIntervalAsync(int seconds)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshInterval", seconds.ToString());
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting refresh interval preference");
        }
    }

    public async Task<Dictionary<string, string>> GetPreferencesAsync()
    {
        try
        {
            var theme = await GetThemeAsync();
            var refreshInterval = await GetRefreshIntervalAsync();
            
            return new Dictionary<string, string>
            {
                { "theme", theme },
                { "refreshInterval", refreshInterval.ToString() }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preferences");
            return new Dictionary<string, string>();
        }
    }

    public async Task SetPreferenceAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

            if (key == "theme")
            {
                await _jsRuntime.InvokeVoidAsync("applyTheme", value);
            }
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting preference {Key}", key);
        }
    }
}

