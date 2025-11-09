using Microsoft.JSInterop;
using System.Text.Json;

namespace AXMonitoringBU.Blazor.Services;

public interface IDashboardService
{
    Task<DashboardLayout> GetDashboardLayoutAsync(string dashboardName);
    Task SaveDashboardLayoutAsync(string dashboardName, DashboardLayout layout);
    Task<List<DashboardWidget>> GetAvailableWidgetsAsync();
    Task<DashboardLayout> CreateCustomDashboardAsync(string name, List<DashboardWidget> widgets);
}

public class DashboardLayout
{
    public string Name { get; set; } = string.Empty;
    public List<DashboardWidget> Widgets { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DashboardWidget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // "metric", "chart", "table", "alert"
    public string Title { get; set; } = string.Empty;
    public int Column { get; set; }
    public int Row { get; set; }
    public int Width { get; set; } = 4; // Bootstrap columns (1-12)
    public int Height { get; set; } = 1; // Grid rows
    public Dictionary<string, object> Config { get; set; } = new();
}

public class DashboardService : IDashboardService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IJSRuntime jsRuntime,
        ILogger<DashboardService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<DashboardLayout> GetDashboardLayoutAsync(string dashboardName)
    {
        try
        {
            var layoutJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", $"dashboard_{dashboardName}");
            if (string.IsNullOrWhiteSpace(layoutJson))
            {
                return GetDefaultLayout();
            }

            var layout = JsonSerializer.Deserialize<DashboardLayout>(layoutJson, GetSerializerOptions());
            if (layout == null || layout.Widgets == null || !layout.Widgets.Any())
            {
                return GetDefaultLayout();
            }

            return layout;
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering
            return GetDefaultLayout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard layout");
            return GetDefaultLayout();
        }
    }

    public async Task SaveDashboardLayoutAsync(string dashboardName, DashboardLayout layout)
    {
        try
        {
            var json = JsonSerializer.Serialize(layout, GetSerializerOptions());
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", $"dashboard_{dashboardName}", json);
            _logger.LogInformation("Saved dashboard layout: {Name}", dashboardName);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - will be called again after render
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving dashboard layout");
        }
    }

    public Task<List<DashboardWidget>> GetAvailableWidgetsAsync()
    {
        var widgets = new List<DashboardWidget>
        {
            new DashboardWidget { Type = "metric", Title = "Batch Backlog", Width = 3, Height = 1 },
            new DashboardWidget { Type = "metric", Title = "Error Rate", Width = 3, Height = 1 },
            new DashboardWidget { Type = "metric", Title = "Active Sessions", Width = 3, Height = 1 },
            new DashboardWidget { Type = "metric", Title = "Blocking Chains", Width = 3, Height = 1 },
            new DashboardWidget { Type = "chart", Title = "CPU Usage", Width = 6, Height = 2 },
            new DashboardWidget { Type = "chart", Title = "Memory Usage", Width = 6, Height = 2 },
            new DashboardWidget { Type = "metric", Title = "SQL Active Connections", Width = 3, Height = 1 },
            new DashboardWidget { Type = "metric", Title = "SQL Longest Query", Width = 3, Height = 1 },
            new DashboardWidget { Type = "table", Title = "Recent Alerts", Width = 12, Height = 3 },
            new DashboardWidget { Type = "table", Title = "Active Sessions Table", Width = 12, Height = 3 }
        };

        return Task.FromResult(widgets);
    }

    public async Task<DashboardLayout> CreateCustomDashboardAsync(string name, List<DashboardWidget> widgets)
    {
        var layout = new DashboardLayout
        {
            Name = name,
            Widgets = widgets,
            CreatedAt = DateTime.UtcNow
        };

        await SaveDashboardLayoutAsync(name, layout);
        return layout;
    }

    private DashboardLayout GetDefaultLayout()
    {
        return new DashboardLayout
        {
            Name = "Default",
            Widgets = new List<DashboardWidget>
            {
                new DashboardWidget { Type = "metric", Title = "Batch Backlog", Column = 0, Row = 0, Width = 3, Height = 1 },
                new DashboardWidget { Type = "metric", Title = "Error Rate", Column = 3, Row = 0, Width = 3, Height = 1 },
                new DashboardWidget { Type = "metric", Title = "Active Sessions", Column = 6, Row = 0, Width = 3, Height = 1 },
                new DashboardWidget { Type = "metric", Title = "Blocking Chains", Column = 9, Row = 0, Width = 3, Height = 1 },
                new DashboardWidget { Type = "chart", Title = "CPU Usage", Column = 0, Row = 1, Width = 6, Height = 2 },
                new DashboardWidget { Type = "chart", Title = "Memory Usage", Column = 6, Row = 1, Width = 6, Height = 2 }
            },
            CreatedAt = DateTime.UtcNow
        };
    }

    private static JsonSerializerOptions GetSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}

