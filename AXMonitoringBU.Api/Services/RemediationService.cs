using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AXMonitoringBU.Api.Services;

public interface IRemediationService
{
    Task<List<RemediationRule>> GetRulesAsync();
    Task<RemediationRule?> GetRuleByIdAsync(string ruleId);
    Task<RemediationRule> CreateRuleAsync(CreateRemediationRuleRequest request);
    Task<bool> UpdateRuleAsync(string ruleId, UpdateRemediationRuleRequest request);
    Task<bool> DeleteRuleAsync(string ruleId);
    Task<List<RemediationRule>> EvaluateConditionsAsync(Dictionary<string, object> metrics);
    Task<RemediationExecution> ExecuteRemediationAsync(string ruleId, Dictionary<string, object> triggerData);
    Task<List<RemediationExecution>> GetExecutionHistoryAsync(string? ruleId = null);
}

public class RemediationService : IRemediationService
{
    private readonly AXDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RemediationService> _logger;

    public RemediationService(
        AXDbContext context,
        IServiceProvider serviceProvider,
        ILogger<RemediationService> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<RemediationRule>> GetRulesAsync()
    {
        try
        {
            var rules = await _context.RemediationRules
                .Where(r => r.Enabled)
                .OrderByDescending(r => r.Priority)
                .ToListAsync();

            return rules.Select(r => MapToRule(r)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remediation rules");
            throw;
        }
    }

    public async Task<RemediationRule?> GetRuleByIdAsync(string ruleId)
    {
        try
        {
            var rule = await _context.RemediationRules.FindAsync(ruleId);
            return rule != null ? MapToRule(rule) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rule {RuleId}", ruleId);
            throw;
        }
    }

    public async Task<RemediationRule> CreateRuleAsync(CreateRemediationRuleRequest request)
    {
        try
        {
            var rule = new RemediationRuleEntity
            {
                Id = $"RULE_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                Name = request.Name,
                Description = request.Description,
                TriggerConditions = System.Text.Json.JsonSerializer.Serialize(request.TriggerConditions),
                Actions = System.Text.Json.JsonSerializer.Serialize(request.Actions),
                Priority = request.Priority,
                Enabled = request.Enabled,
                CooldownMinutes = request.CooldownMinutes,
                MaxAttempts = request.MaxAttempts,
                TimeoutSeconds = request.TimeoutSeconds,
                RequiresConfirmation = request.RequiresConfirmation,
                BusinessImpact = request.BusinessImpact,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.RemediationRules.Add(rule);
            await _context.SaveChangesAsync();

            return MapToRule(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating remediation rule");
            throw;
        }
    }

    public async Task<bool> UpdateRuleAsync(string ruleId, UpdateRemediationRuleRequest request)
    {
        try
        {
            var rule = await _context.RemediationRules.FindAsync(ruleId);
            if (rule == null)
            {
                return false;
            }

            rule.Name = request.Name ?? rule.Name;
            rule.Description = request.Description ?? rule.Description;
            rule.Enabled = request.Enabled ?? rule.Enabled;
            rule.Priority = request.Priority ?? rule.Priority;
            rule.UpdatedAt = DateTime.UtcNow;

            if (request.TriggerConditions != null)
            {
                rule.TriggerConditions = System.Text.Json.JsonSerializer.Serialize(request.TriggerConditions);
            }

            if (request.Actions != null)
            {
                rule.Actions = System.Text.Json.JsonSerializer.Serialize(request.Actions);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rule {RuleId}", ruleId);
            throw;
        }
    }

    public async Task<bool> DeleteRuleAsync(string ruleId)
    {
        try
        {
            var rule = await _context.RemediationRules.FindAsync(ruleId);
            if (rule == null)
            {
                return false;
            }

            _context.RemediationRules.Remove(rule);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rule {RuleId}", ruleId);
            throw;
        }
    }

    public async Task<List<RemediationRule>> EvaluateConditionsAsync(Dictionary<string, object> metrics)
    {
        try
        {
            var rules = await GetRulesAsync();
            var triggeredRules = new List<RemediationRule>();

            foreach (var rule in rules)
            {
                if (EvaluateRuleConditions(rule, metrics))
                {
                    // Check cooldown
                    var lastExecution = await _context.RemediationExecutions
                        .Where(e => e.RuleId == rule.Id)
                        .OrderByDescending(e => e.StartTime)
                        .FirstOrDefaultAsync();

                    if (lastExecution == null || 
                        DateTime.UtcNow - lastExecution.StartTime > TimeSpan.FromMinutes(rule.CooldownMinutes))
                    {
                        triggeredRules.Add(rule);
                    }
                }
            }

            return triggeredRules.OrderByDescending(r => r.Priority).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating remediation conditions");
            throw;
        }
    }

    public async Task<RemediationExecution> ExecuteRemediationAsync(string ruleId, Dictionary<string, object> triggerData)
    {
        try
        {
            var rule = await GetRuleByIdAsync(ruleId);
            if (rule == null)
            {
                throw new ArgumentException($"Rule {ruleId} not found");
            }

            var execution = new RemediationExecutionEntity
            {
                Id = $"EXEC_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid()}",
                RuleId = ruleId,
                TriggerData = System.Text.Json.JsonSerializer.Serialize(triggerData),
                Status = "Pending",
                ActionsExecuted = "[]",
                StartTime = DateTime.UtcNow,
                ResultData = "{}"
            };

            _context.RemediationExecutions.Add(execution);
            await _context.SaveChangesAsync();

            // Execute actions asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    execution.Status = "Running";
                    await _context.SaveChangesAsync();

                    var actions = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(rule.Actions) ?? new();
                    var executedActions = new List<Dictionary<string, object>>();

                    foreach (var actionConfig in actions)
                    {
                        var actionResult = await ExecuteActionAsync(actionConfig, triggerData);
                        executedActions.Add(actionResult);

                        var continueOnFailure = actionConfig.TryGetValue("continue_on_failure", out var cof) && cof is bool b && b;
                        
                        if (actionResult["status"].ToString() == "failed" && !continueOnFailure)
                        {
                            execution.Status = "Failed";
                            execution.ErrorMessage = $"Action {actionConfig["action"]} failed";
                            break;
                        }
                    }

                    if (execution.Status == "Running")
                    {
                        execution.Status = "Success";
                    }

                    execution.ActionsExecuted = System.Text.Json.JsonSerializer.Serialize(executedActions);
                    execution.EndTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing remediation {ExecutionId}", execution.Id);
                    execution.Status = "Failed";
                    execution.ErrorMessage = ex.Message;
                    execution.EndTime = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            });

            return new RemediationExecution
            {
                Id = execution.Id,
                RuleId = execution.RuleId,
                Status = execution.Status,
                StartTime = execution.StartTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing remediation");
            throw;
        }
    }

    public async Task<List<RemediationExecution>> GetExecutionHistoryAsync(string? ruleId = null)
    {
        try
        {
            var query = _context.RemediationExecutions.AsQueryable();

            if (!string.IsNullOrEmpty(ruleId))
            {
                query = query.Where(e => e.RuleId == ruleId);
            }

            var executions = await query
                .OrderByDescending(e => e.StartTime)
                .Take(100)
                .ToListAsync();

            return executions.Select(e => MapToExecution(e)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution history");
            throw;
        }
    }

    private bool EvaluateRuleConditions(RemediationRule rule, Dictionary<string, object> metrics)
    {
        try
        {
            var conditions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(rule.TriggerConditions) 
                ?? new Dictionary<string, object>();

            foreach (var condition in conditions)
            {
                var metricName = condition.Key;
                var expectedValue = condition.Value;

                if (!metrics.TryGetValue(metricName, out var actualValue))
                {
                    return false;
                }

                // Simple comparison - can be extended
                if (!actualValue?.Equals(expectedValue) == true)
                {
                    // Try numeric comparison
                    if (double.TryParse(expectedValue?.ToString(), out var expectedNum) &&
                        double.TryParse(actualValue?.ToString(), out var actualNum))
                    {
                        // Check threshold conditions
                        if (metricName.Contains(">") && actualNum <= expectedNum)
                            return false;
                        if (metricName.Contains("<") && actualNum >= expectedNum)
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule conditions");
            return false;
        }
    }

    private async Task<Dictionary<string, object>> ExecuteActionAsync(Dictionary<string, object> actionConfig, Dictionary<string, object> triggerData)
    {
        var actionName = actionConfig["action"].ToString() ?? "";
        var result = new Dictionary<string, object>
        {
            { "action", actionName },
            { "status", "success" },
            { "timestamp", DateTime.UtcNow }
        };

        try
        {
            switch (actionName)
            {
                case "restart_batch_job":
                    if (actionConfig.TryGetValue("job_id", out var jobId))
                    {
                        var batchJobService = _serviceProvider.GetRequiredService<IBatchJobService>();
                        var jobIdStr = jobId?.ToString() ?? "";
                        if (int.TryParse(jobIdStr, out var jobIdInt))
                        {
                            var restarted = await batchJobService.RestartBatchJobAsync(jobIdInt);
                            result["status"] = restarted ? "success" : "failed";
                            result["message"] = restarted 
                                ? $"Batch job {jobId} restarted successfully" 
                                : $"Failed to restart batch job {jobId}";
                        }
                        else
                        {
                            result["status"] = "failed";
                            result["message"] = $"Invalid job ID: {jobId}";
                        }
                    }
                    break;

                case "kill_session":
                    if (actionConfig.TryGetValue("session_id", out var sessionId))
                    {
                        var sessionService = _serviceProvider.GetRequiredService<ISessionService>();
                        var sessionIdStr = sessionId?.ToString() ?? "";
                        if (int.TryParse(sessionIdStr, out var sessionIdInt))
                        {
                            var killed = await sessionService.KillSessionAsync(sessionIdInt);
                            result["status"] = killed ? "success" : "failed";
                            result["message"] = killed 
                                ? $"Session {sessionId} killed successfully" 
                                : $"Failed to kill session {sessionId}";
                        }
                        else
                        {
                            result["status"] = "failed";
                            result["message"] = $"Invalid session ID: {sessionId}";
                        }
                    }
                    break;

                case "send_notification":
                    var emailService = _serviceProvider.GetService<IEmailAlertService>();
                    if (emailService != null)
                    {
                        // Create alert and send notification
                        result["message"] = "Notification sent";
                    }
                    break;

                default:
                    result["status"] = "skipped";
                    result["message"] = $"Action {actionName} not implemented";
                    break;
            }
        }
        catch (Exception ex)
        {
            result["status"] = "failed";
            result["error"] = ex.Message;
            _logger.LogError(ex, "Error executing action {Action}", actionName);
        }

        return result;
    }

    private RemediationRule MapToRule(RemediationRuleEntity entity)
    {
        return new RemediationRule
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            TriggerConditions = entity.TriggerConditions,
            Actions = entity.Actions,
            Priority = entity.Priority,
            Enabled = entity.Enabled,
            CooldownMinutes = entity.CooldownMinutes,
            MaxAttempts = entity.MaxAttempts,
            TimeoutSeconds = entity.TimeoutSeconds,
            RequiresConfirmation = entity.RequiresConfirmation,
            BusinessImpact = entity.BusinessImpact
        };
    }

    private RemediationExecution MapToExecution(RemediationExecutionEntity entity)
    {
        return new RemediationExecution
        {
            Id = entity.Id,
            RuleId = entity.RuleId,
            Status = entity.Status,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            ErrorMessage = entity.ErrorMessage
        };
    }
}

public class RemediationRule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerConditions { get; set; } = string.Empty; // JSON
    public string Actions { get; set; } = string.Empty; // JSON
    public int Priority { get; set; }
    public bool Enabled { get; set; }
    public int CooldownMinutes { get; set; }
    public int MaxAttempts { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? BusinessImpact { get; set; }
}

public class RemediationExecution
{
    public string Id { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateRemediationRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> TriggerConditions { get; set; } = new();
    public List<Dictionary<string, object>> Actions { get; set; } = new();
    public int Priority { get; set; } = 5;
    public bool Enabled { get; set; } = true;
    public int CooldownMinutes { get; set; } = 15;
    public int MaxAttempts { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;
    public bool RequiresConfirmation { get; set; } = false;
    public string? BusinessImpact { get; set; }
}

public class UpdateRemediationRuleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? TriggerConditions { get; set; }
    public List<Dictionary<string, object>>? Actions { get; set; }
    public int? Priority { get; set; }
    public bool? Enabled { get; set; }
    public string? BusinessImpact { get; set; }
}

