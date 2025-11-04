using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Services;

public interface IPredictionsService
{
    Task<BatchRuntimePredictionResponse?> PredictBatchRuntimeAsync(BatchRuntimePredictionRequest request);
    Task<ResourceUsagePredictionResponse?> PredictResourceUsageAsync(ResourceUsagePredictionRequest request);
    Task<AnomalyDetectionResponse?> DetectAnomaliesAsync(AnomalyDetectionRequest request);
}

public class PredictionsService : IPredictionsService
{
    private readonly IApiService _apiService;

    public PredictionsService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<BatchRuntimePredictionResponse?> PredictBatchRuntimeAsync(BatchRuntimePredictionRequest request)
    {
        return await _apiService.PostAsync<BatchRuntimePredictionResponse>("api/v1/predictions/batch-runtime", request);
    }

    public async Task<ResourceUsagePredictionResponse?> PredictResourceUsageAsync(ResourceUsagePredictionRequest request)
    {
        return await _apiService.PostAsync<ResourceUsagePredictionResponse>("api/v1/predictions/resource-usage", request);
    }

    public async Task<AnomalyDetectionResponse?> DetectAnomaliesAsync(AnomalyDetectionRequest request)
    {
        return await _apiService.PostAsync<AnomalyDetectionResponse>("api/v1/predictions/anomalies", request);
    }
}

public class BatchRuntimePredictionRequest
{
    public int JobComplexity { get; set; }
    public int DataVolume { get; set; }
    public int AosLoad { get; set; }
    public string TimeOfDay { get; set; } = "0.5";
}

public class BatchRuntimePredictionResponse
{
    public double predicted_runtime_minutes { get; set; }
    public string confidence { get; set; } = string.Empty;
    public string model_version { get; set; } = string.Empty;
    public double r2_score { get; set; }
    public double mae { get; set; }
    public List<KeyFactor> key_factors { get; set; } = new();
    public DateTime timestamp { get; set; }
}

public class KeyFactor
{
    public string factor { get; set; } = string.Empty;
    public double importance { get; set; }
}

public class ResourceUsagePredictionRequest
{
    public string? Metric { get; set; }
    public int? HorizonHours { get; set; } = 24;
}

public class ResourceUsagePredictionResponse
{
    public ResourceUsagePredictions predictions { get; set; } = new();
    public string confidence { get; set; } = string.Empty;
    public DateTime timestamp { get; set; }
}

public class ResourceUsagePredictions
{
    public double cpu_usage_1h { get; set; }
    public double cpu_usage_24h { get; set; }
    public double memory_usage_1h { get; set; }
    public double memory_usage_24h { get; set; }
}

public class AnomalyDetectionRequest
{
    public string? Metric { get; set; }
    public int? LookbackHours { get; set; } = 24;
    public double? Threshold { get; set; }
}

public class AnomalyDetectionResponse
{
    public List<AnomalyResult> anomalies { get; set; } = new();
    public int count { get; set; }
    public DateTime timestamp { get; set; }
}

public class AnomalyResult
{
    public string metric { get; set; } = string.Empty;
    public DateTime timestamp { get; set; }
    public double value { get; set; }
    public ExpectedRange expected_range { get; set; } = new();
    public string severity { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
}

public class ExpectedRange
{
    public double min { get; set; }
    public double max { get; set; }
}

