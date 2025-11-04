using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IMetricsService
{
    Task<MetricsResponse?> GetCurrentMetricsAsync();
    Task<MetricsHistoryResponse?> GetMetricsHistoryAsync(string? metric = null, string timeRange = "24h");
    Task<BusinessKpisResponse?> GetBusinessKpisAsync();
    Task<BusinessImpactReportDto?> GetBusinessImpactAsync();
    Task<object?> GenerateReportAsync(string endpoint, string period);
}

public class MetricsService : IMetricsService
{
    private readonly IApiService _apiService;

    public MetricsService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<MetricsResponse?> GetCurrentMetricsAsync()
    {
        return await _apiService.GetAsync<MetricsResponse>("api/v1/metrics/current");
    }

    public async Task<MetricsHistoryResponse?> GetMetricsHistoryAsync(string? metric = null, string timeRange = "24h")
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(metric))
            queryParams.Add($"metric={Uri.EscapeDataString(metric)}");
        queryParams.Add($"timeRange={Uri.EscapeDataString(timeRange)}");
        
        var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
        return await _apiService.GetAsync<MetricsHistoryResponse>($"api/v1/metrics/history{queryString}");
    }

    public async Task<BusinessKpisResponse?> GetBusinessKpisAsync()
    {
        return await _apiService.GetAsync<BusinessKpisResponse>("api/v1/metrics/kpis");
    }

    public async Task<BusinessImpactReportDto?> GetBusinessImpactAsync()
    {
        return await _apiService.GetAsync<BusinessImpactReportDto>("api/v1/metrics/business-impact");
    }

    public async Task<object?> GenerateReportAsync(string endpoint, string period)
    {
        return await _apiService.PostAsync<object>(endpoint, new { period });
    }
}

public class MetricsResponse
{
    public KpiData? kpis { get; set; }
    public SqlHealthData? sql_health { get; set; }
    public DateTime timestamp { get; set; }
}

public class KpiData
{
    public int batch_backlog { get; set; }
    public double error_rate { get; set; }
    public int active_sessions { get; set; }
    public int blocking_chains { get; set; }
}

public class SqlHealthData
{
    public double cpu_usage { get; set; }
    public double memory_usage { get; set; }
    public double io_wait { get; set; }
    public double tempdb_usage { get; set; }
    public int active_connections { get; set; }
    public int longest_running_query { get; set; }
}

public class BusinessKpisResponse
{
    public Dictionary<string, BusinessKpiResultDto>? business_kpis { get; set; }
    public DateTime timestamp { get; set; }
}

public class BusinessKpiResultDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double TargetValue { get; set; }
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BusinessImpact { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}

public class BusinessImpactReportDto
{
    public double OverallBusinessHealth { get; set; }
    public double SlaCompliancePercentage { get; set; }
    public int CriticalIssuesCount { get; set; }
    public List<string> CriticalIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class MetricsHistoryResponse
{
    public string metric { get; set; } = string.Empty;
    public string time_range { get; set; } = string.Empty;
    public List<MetricsHistoryDataPoint> data { get; set; } = new();
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class MetricsHistoryDataPoint
{
    public DateTime timestamp { get; set; }
    public double value { get; set; }
}

