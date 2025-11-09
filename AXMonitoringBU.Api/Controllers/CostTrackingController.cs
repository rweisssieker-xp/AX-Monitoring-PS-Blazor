using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for cost tracking and optimization
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/costs")]
public class CostTrackingController : ControllerBase
{
    private readonly ICostTrackingService _costService;
    private readonly ILogger<CostTrackingController> _logger;

    public CostTrackingController(
        ICostTrackingService costService,
        ILogger<CostTrackingController> logger)
    {
        _costService = costService;
        _logger = logger;
    }

    /// <summary>
    /// Get cost tracking records
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCosts(
        [FromQuery] string? resourceType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var costs = await _costService.GetCostsAsync(resourceType, startDate, endDate);
            var totalCost = await _costService.GetTotalCostAsync(resourceType, startDate, endDate);
            
            return Ok(new
            {
                costs,
                total_cost = totalCost,
                count = costs.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting costs");
            return StatusCode(500, new { error = "Failed to retrieve costs" });
        }
    }

    /// <summary>
    /// Get total cost
    /// </summary>
    [HttpGet("total")]
    public async Task<IActionResult> GetTotalCost(
        [FromQuery] string? resourceType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var totalCost = await _costService.GetTotalCostAsync(resourceType, startDate, endDate);
            return Ok(new { total_cost = totalCost });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total cost");
            return StatusCode(500, new { error = "Failed to calculate total cost" });
        }
    }

    /// <summary>
    /// Get costs for a specific resource
    /// </summary>
    [HttpGet("resources/{resourceType}/{resourceId}")]
    public async Task<IActionResult> GetResourceCosts(string resourceType, string resourceId)
    {
        try
        {
            var costs = await _costService.GetCostsByResourceAsync(resourceType, resourceId);
            var totalCost = costs.Sum(c => c.Cost);
            
            return Ok(new
            {
                costs,
                total_cost = totalCost,
                count = costs.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting costs for resource {ResourceType}/{ResourceId}", resourceType, resourceId);
            return StatusCode(500, new { error = "Failed to retrieve resource costs" });
        }
    }

    /// <summary>
    /// Record a cost
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RecordCost([FromBody] CostTracking cost)
    {
        try
        {
            var recorded = await _costService.RecordCostAsync(cost);
            return CreatedAtAction(nameof(GetResourceCosts), 
                new { resourceType = cost.ResourceType, resourceId = cost.ResourceId }, 
                recorded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cost");
            return StatusCode(500, new { error = "Failed to record cost" });
        }
    }

    /// <summary>
    /// Get cost breakdown by resource type
    /// </summary>
    [HttpGet("breakdown")]
    public async Task<IActionResult> GetCostBreakdown(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var breakdown = await _costService.GetCostBreakdownAsync(startDate, endDate);
            return Ok(new { breakdown });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost breakdown");
            return StatusCode(500, new { error = "Failed to retrieve cost breakdown" });
        }
    }

    /// <summary>
    /// Get cost optimization recommendations
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] bool? implemented = null,
        [FromQuery] string? priority = null)
    {
        try
        {
            var recommendations = await _costService.GetRecommendationsAsync(implemented, priority);
            return Ok(new { recommendations, count = recommendations.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return StatusCode(500, new { error = "Failed to retrieve recommendations" });
        }
    }

    /// <summary>
    /// Create a cost optimization recommendation
    /// </summary>
    [HttpPost("recommendations")]
    public async Task<IActionResult> CreateRecommendation([FromBody] CostOptimizationRecommendation recommendation)
    {
        try
        {
            var created = await _costService.CreateRecommendationAsync(recommendation);
            return CreatedAtAction(nameof(GetRecommendations), new { }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recommendation");
            return StatusCode(500, new { error = "Failed to create recommendation" });
        }
    }

    /// <summary>
    /// Mark a recommendation as implemented
    /// </summary>
    [HttpPost("recommendations/{id}/implement")]
    public async Task<IActionResult> ImplementRecommendation(int id)
    {
        try
        {
            var username = User.Identity?.Name ?? "System";
            var success = await _costService.ImplementRecommendationAsync(id, username);
            if (!success)
            {
                return NotFound();
            }
            return Ok(new { message = "Recommendation marked as implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error implementing recommendation {RecommendationId}", id);
            return StatusCode(500, new { error = "Failed to implement recommendation" });
        }
    }

    /// <summary>
    /// Get cost budgets
    /// </summary>
    [HttpGet("budgets")]
    public async Task<IActionResult> GetBudgets([FromQuery] bool? active = null)
    {
        try
        {
            var budgets = await _costService.GetBudgetsAsync(active);
            return Ok(new { budgets, count = budgets.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting budgets");
            return StatusCode(500, new { error = "Failed to retrieve budgets" });
        }
    }

    /// <summary>
    /// Create a cost budget
    /// </summary>
    [HttpPost("budgets")]
    public async Task<IActionResult> CreateBudget([FromBody] CostBudget budget)
    {
        try
        {
            var created = await _costService.CreateBudgetAsync(budget);
            return CreatedAtAction(nameof(GetBudgets), new { }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating budget");
            return StatusCode(500, new { error = "Failed to create budget" });
        }
    }

    /// <summary>
    /// Update a cost budget
    /// </summary>
    [HttpPut("budgets/{id}")]
    public async Task<IActionResult> UpdateBudget(int id, [FromBody] CostBudget budget)
    {
        try
        {
            var success = await _costService.UpdateBudgetAsync(id, budget);
            if (!success)
            {
                return NotFound();
            }
            return Ok(new { message = "Budget updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budget {BudgetId}", id);
            return StatusCode(500, new { error = "Failed to update budget" });
        }
    }
}

