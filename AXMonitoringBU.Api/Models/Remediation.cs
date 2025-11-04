using System.ComponentModel.DataAnnotations;

namespace AXMonitoringBU.Api.Models;

public class RemediationRuleEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TriggerConditions { get; set; } = string.Empty; // JSON
    public string Actions { get; set; } = string.Empty; // JSON
    public int Priority { get; set; } = 5;
    public bool Enabled { get; set; } = true;
    public int CooldownMinutes { get; set; } = 15;
    public int MaxAttempts { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;
    public bool RequiresConfirmation { get; set; } = false;
    public string? BusinessImpact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RemediationExecutionEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string TriggerData { get; set; } = string.Empty; // JSON
    public string Status { get; set; } = string.Empty;
    public string ActionsExecuted { get; set; } = string.Empty; // JSON
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string ResultData { get; set; } = string.Empty; // JSON
}

