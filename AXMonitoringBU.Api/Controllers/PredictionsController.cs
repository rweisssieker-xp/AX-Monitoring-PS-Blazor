using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/predictions")]
public class PredictionsController : ControllerBase
{
    private readonly ILogger<PredictionsController> _logger;
    private readonly IKpiDataService _kpiDataService;

    public PredictionsController(ILogger<PredictionsController> logger, IKpiDataService kpiDataService)
    {
        _logger = logger;
        _kpiDataService = kpiDataService;
    }

    /// <summary>
    /// Predict batch job runtime
    /// </summary>
    [HttpPost("batch-runtime")]
    public IActionResult PredictBatchRuntime([FromBody] BatchRuntimePredictionRequest request)
    {
        try
        {
            // Mock prediction - in production, this would call ML service
            var predictedRuntime = CalculatePredictedRuntime(request);
            
            return Ok(new
            {
                predicted_runtime_minutes = predictedRuntime,
                confidence = "High",
                model_version = "1.0",
                r2_score = 0.87,
                mae = 3.2,
                key_factors = new[]
                {
                    new { factor = "Data Volume", importance = 0.45 },
                    new { factor = "Job Complexity", importance = 0.30 },
                    new { factor = "AOS Load", importance = 0.15 },
                    new { factor = "Time of Day", importance = 0.10 }
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting batch runtime");
            return StatusCode(500, new { error = "Failed to predict batch runtime" });
        }
    }

    /// <summary>
    /// Predict resource usage
    /// </summary>
    [HttpPost("resource-usage")]
    public async Task<IActionResult> PredictResourceUsage([FromBody] ResourceUsagePredictionRequest request)
    {
        try
        {
            // Mock prediction
            var currentMetrics = await _kpiDataService.GetKpiDataAsync();
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();

            var cpuUsage = sqlHealth?.TryGetValue("cpu_usage", out var cpu) == true ? Convert.ToDouble(cpu) : 50.0;
            var memoryUsage = sqlHealth?.TryGetValue("memory_usage", out var memory) == true ? Convert.ToDouble(memory) : 60.0;

            return Ok(new
            {
                predictions = new
                {
                    cpu_usage_1h = cpuUsage + 5,
                    cpu_usage_24h = cpuUsage + 10,
                    memory_usage_1h = memoryUsage + 3,
                    memory_usage_24h = memoryUsage + 8
                },
                confidence = "Medium",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting resource usage");
            return StatusCode(500, new { error = "Failed to predict resource usage" });
        }
    }

    /// <summary>
    /// Detect anomalies
    /// </summary>
    [HttpPost("anomalies")]
    public IActionResult DetectAnomalies([FromBody] AnomalyDetectionRequest request)
    {
        try
        {
            // Mock anomaly detection
            var anomalies = new List<AnomalyResult>
            {
                new AnomalyResult
                {
                    metric = "cpu_usage",
                    timestamp = DateTime.UtcNow.AddHours(-2),
                    value = 85.5,
                    expected_range = new { min = 40.0, max = 70.0 },
                    severity = "high",
                    description = "CPU usage spike detected"
                }
            };

            return Ok(new
            {
                anomalies = anomalies,
                count = anomalies.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
            return StatusCode(500, new { error = "Failed to detect anomalies" });
        }
    }

    private double CalculatePredictedRuntime(BatchRuntimePredictionRequest request)
    {
        // Simple mock calculation
        var baseRuntime = 30.0;
        var complexityFactor = request.JobComplexity / 100.0 * 20;
        var volumeFactor = request.DataVolume / 100.0 * 15;
        var loadFactor = request.AosLoad / 100.0 * 10;
        var timeFactor = double.Parse(request.TimeOfDay) * 5;

        return baseRuntime + complexityFactor + volumeFactor + loadFactor + timeFactor;
    }
}

public class BatchRuntimePredictionRequest
{
    public int JobComplexity { get; set; }
    public int DataVolume { get; set; }
    public int AosLoad { get; set; }
    public string TimeOfDay { get; set; } = "0.5";
}

public class ResourceUsagePredictionRequest
{
    public string? Metric { get; set; }
    public int? HorizonHours { get; set; } = 24;
}

public class AnomalyDetectionRequest
{
    public string? Metric { get; set; }
    public int? LookbackHours { get; set; } = 24;
    public double? Threshold { get; set; }
}

public class AnomalyResult
{
    public string metric { get; set; } = string.Empty;
    public DateTime timestamp { get; set; }
    public double value { get; set; }
    public object expected_range { get; set; } = new { };
    public string severity { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
}
