using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

/// <summary>
/// Controller for managing alert escalation rules and escalations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/alerts/escalation")]
public class AlertEscalationController : ControllerBase
{
    private readonly IAlertEscalationService _escalationService;
    private readonly ILogger<AlertEscalationController> _logger;

    public AlertEscalationController(
        IAlertEscalationService escalationService,
        ILogger<AlertEscalationController> logger)
    {
        _escalationService = escalationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all escalation rules
    /// </summary>
    [HttpGet("rules")]
    public async Task<IActionResult> GetEscalationRules([FromQuery] bool? enabled = null)
    {
        try
        {
            var rules = await _escalationService.GetEscalationRulesAsync(enabled);
            return Ok(new { rules, count = rules.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation rules");
            return StatusCode(500, new { error = "Failed to retrieve escalation rules" });
        }
    }

    /// <summary>
    /// Get escalation rule by ID
    /// </summary>
    [HttpGet("rules/{id}")]
    public async Task<IActionResult> GetEscalationRule(int id)
    {
        try
        {
            var rule = await _escalationService.GetEscalationRuleByIdAsync(id);
            if (rule == null)
            {
                return NotFound();
            }
            return Ok(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation rule {RuleId}", id);
            return StatusCode(500, new { error = "Failed to retrieve escalation rule" });
        }
    }

    /// <summary>
    /// Create a new escalation rule
    /// </summary>
    [HttpPost("rules")]
    public async Task<IActionResult> CreateEscalationRule([FromBody] AlertEscalationRule rule)
    {
        try
        {
            rule.CreatedBy = User.Identity?.Name ?? "System";
            var created = await _escalationService.CreateEscalationRuleAsync(rule);
            return CreatedAtAction(nameof(GetEscalationRule), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating escalation rule");
            return StatusCode(500, new { error = "Failed to create escalation rule" });
        }
    }

    /// <summary>
    /// Update an escalation rule
    /// </summary>
    [HttpPut("rules/{id}")]
    public async Task<IActionResult> UpdateEscalationRule(int id, [FromBody] AlertEscalationRule rule)
    {
        try
        {
            var success = await _escalationService.UpdateEscalationRuleAsync(id, rule);
            if (!success)
            {
                return NotFound();
            }
            return Ok(new { message = "Escalation rule updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating escalation rule {RuleId}", id);
            return StatusCode(500, new { error = "Failed to update escalation rule" });
        }
    }

    /// <summary>
    /// Delete an escalation rule
    /// </summary>
    [HttpDelete("rules/{id}")]
    public async Task<IActionResult> DeleteEscalationRule(int id)
    {
        try
        {
            var success = await _escalationService.DeleteEscalationRuleAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return Ok(new { message = "Escalation rule deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting escalation rule {RuleId}", id);
            return StatusCode(500, new { error = "Failed to delete escalation rule" });
        }
    }

    /// <summary>
    /// Get escalations for a specific alert
    /// </summary>
    [HttpGet("alerts/{alertId}")]
    public async Task<IActionResult> GetEscalationsForAlert(int alertId)
    {
        try
        {
            var escalations = await _escalationService.GetEscalationsForAlertAsync(alertId);
            return Ok(new { escalations, count = escalations.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalations for alert {AlertId}", alertId);
            return StatusCode(500, new { error = "Failed to retrieve escalations" });
        }
    }
}

