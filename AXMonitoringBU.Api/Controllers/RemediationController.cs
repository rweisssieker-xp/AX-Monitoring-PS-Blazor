using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/remediation")]
public class RemediationController : ControllerBase
{
    private readonly IRemediationService _remediationService;
    private readonly IKpiDataService _kpiDataService;
    private readonly ILogger<RemediationController> _logger;

    public RemediationController(
        IRemediationService remediationService,
        IKpiDataService kpiDataService,
        ILogger<RemediationController> logger)
    {
        _remediationService = remediationService;
        _kpiDataService = kpiDataService;
        _logger = logger;
    }

    /// <summary>
    /// Get all remediation rules
    /// </summary>
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        try
        {
            var rules = await _remediationService.GetRulesAsync();
            return Ok(new
            {
                rules = rules,
                count = rules.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remediation rules");
            return StatusCode(500, new { error = "Failed to retrieve remediation rules" });
        }
    }

    /// <summary>
    /// Get remediation rule by ID
    /// </summary>
    [HttpGet("rules/{ruleId}")]
    public async Task<IActionResult> GetRule(string ruleId)
    {
        try
        {
            var rule = await _remediationService.GetRuleByIdAsync(ruleId);
            if (rule == null)
            {
                return NotFound(new { message = $"Rule {ruleId} not found" });
            }
            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Failed to retrieve rule" });
        }
    }

    /// <summary>
    /// Create a new remediation rule
    /// </summary>
    [HttpPost("rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreateRemediationRuleRequest request)
    {
        try
        {
            var rule = await _remediationService.CreateRuleAsync(request);
            return CreatedAtAction(nameof(GetRule), new { ruleId = rule.Id }, rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating remediation rule");
            return StatusCode(500, new { error = "Failed to create remediation rule" });
        }
    }

    /// <summary>
    /// Update a remediation rule
    /// </summary>
    [HttpPut("rules/{ruleId}")]
    public async Task<IActionResult> UpdateRule(string ruleId, [FromBody] UpdateRemediationRuleRequest request)
    {
        try
        {
            var success = await _remediationService.UpdateRuleAsync(ruleId, request);
            if (!success)
            {
                return NotFound(new { message = $"Rule {ruleId} not found" });
            }
            return Ok(new { message = "Rule updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Failed to update rule" });
        }
    }

    /// <summary>
    /// Delete a remediation rule
    /// </summary>
    [HttpDelete("rules/{ruleId}")]
    public async Task<IActionResult> DeleteRule(string ruleId)
    {
        try
        {
            var success = await _remediationService.DeleteRuleAsync(ruleId);
            if (!success)
            {
                return NotFound(new { message = $"Rule {ruleId} not found" });
            }
            return Ok(new { message = "Rule deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {RuleId}", ruleId);
            return StatusCode(500, new { error = "Failed to delete rule" });
        }
    }

    /// <summary>
    /// Evaluate conditions and get triggered rules
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateConditions([FromBody] EvaluateConditionsRequest request)
    {
        try
        {
            var metrics = request.Metrics ?? await GetCurrentMetricsAsync();
            var triggeredRules = await _remediationService.EvaluateConditionsAsync(metrics);

            return Ok(new
            {
                triggered_rules = triggeredRules,
                count = triggeredRules.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating conditions");
            return StatusCode(500, new { error = "Failed to evaluate conditions" });
        }
    }

    /// <summary>
    /// Execute remediation for a rule
    /// </summary>
    [HttpPost("execute/{ruleId}")]
    public async Task<IActionResult> ExecuteRemediation(string ruleId, [FromBody] ExecuteRemediationRequest request)
    {
        try
        {
            var triggerData = request.TriggerData ?? await GetCurrentMetricsAsync();
            var execution = await _remediationService.ExecuteRemediationAsync(ruleId, triggerData);

            return Ok(new
            {
                execution = execution,
                message = "Remediation execution started",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing remediation");
            return StatusCode(500, new { error = "Failed to execute remediation" });
        }
    }

    /// <summary>
    /// Get execution history
    /// </summary>
    [HttpGet("executions")]
    public async Task<IActionResult> GetExecutionHistory([FromQuery] string? ruleId = null)
    {
        try
        {
            var executions = await _remediationService.GetExecutionHistoryAsync(ruleId);
            return Ok(new
            {
                executions = executions,
                count = executions.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution history");
            return StatusCode(500, new { error = "Failed to retrieve execution history" });
        }
    }

    private async Task<Dictionary<string, object>> GetCurrentMetricsAsync()
    {
        var kpiData = await _kpiDataService.GetKpiDataAsync();
        var sqlHealth = await _kpiDataService.GetSqlHealthAsync();

        var metrics = new Dictionary<string, object>();
        if (kpiData != null)
        {
            foreach (var kvp in kpiData)
            {
                metrics[kvp.Key] = kvp.Value;
            }
        }
        if (sqlHealth != null)
        {
            foreach (var kvp in sqlHealth)
            {
                metrics[kvp.Key] = kvp.Value;
            }
        }
        return metrics;
    }
}

public class EvaluateConditionsRequest
{
    public Dictionary<string, object>? Metrics { get; set; }
}

public class ExecuteRemediationRequest
{
    public Dictionary<string, object>? TriggerData { get; set; }
}

