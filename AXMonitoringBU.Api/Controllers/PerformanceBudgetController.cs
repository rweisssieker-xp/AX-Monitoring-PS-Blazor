using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/performance-budgets")]
public class PerformanceBudgetController : ControllerBase
{
    private readonly IPerformanceBudgetService _performanceBudgetService;
    private readonly ILogger<PerformanceBudgetController> _logger;

    public PerformanceBudgetController(
        IPerformanceBudgetService performanceBudgetService,
        ILogger<PerformanceBudgetController> logger)
    {
        _performanceBudgetService = performanceBudgetService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPerformanceBudgets()
    {
        try
        {
            var budgets = await _performanceBudgetService.GetPerformanceBudgetsAsync();
            return Ok(new
            {
                performance_budgets = budgets,
                count = budgets.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance budgets");
            return StatusCode(500, new { error = "Failed to retrieve performance budgets" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetPerformanceBudget([FromBody] SetPerformanceBudgetRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Endpoint) || request.P95ThresholdMs <= 0)
            {
                return BadRequest(new { error = "Endpoint and P95ThresholdMs (positive) are required" });
            }

            var budget = await _performanceBudgetService.SetPerformanceBudgetAsync(
                request.Endpoint, 
                request.P95ThresholdMs);

            return Ok(new
            {
                performance_budget = budget,
                message = "Performance budget set successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting performance budget");
            return StatusCode(500, new { error = "Failed to set performance budget" });
        }
    }

    [HttpPost("check")]
    public async Task<IActionResult> CheckPerformanceBudget([FromBody] CheckPerformanceBudgetRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Endpoint) || request.DurationMs <= 0)
            {
                return BadRequest(new { error = "Endpoint and DurationMs (positive) are required" });
            }

            var duration = TimeSpan.FromMilliseconds(request.DurationMs);
            var result = await _performanceBudgetService.CheckPerformanceBudgetAsync(
                request.Endpoint, 
                duration);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking performance budget");
            return StatusCode(500, new { error = "Failed to check performance budget" });
        }
    }
}

public class SetPerformanceBudgetRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public double P95ThresholdMs { get; set; }
}

public class CheckPerformanceBudgetRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public double DurationMs { get; set; }
}

