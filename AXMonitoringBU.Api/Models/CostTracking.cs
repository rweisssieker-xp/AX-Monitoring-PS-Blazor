namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Tracks resource costs for batch jobs and system resources
/// </summary>
public class CostTracking
{
    public int Id { get; set; }
    
    /// <summary>
    /// Resource type: "BatchJob", "Session", "Storage", "Compute", "Network"
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Resource identifier (e.g., Batch Job ID, Session ID)
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Resource name
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Cost in the configured currency (default: USD)
    /// </summary>
    public decimal Cost { get; set; }
    
    /// <summary>
    /// Currency code (e.g., "USD", "EUR")
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Cost breakdown JSON (CPU, Memory, Storage, etc.)
    /// </summary>
    public string? CostBreakdown { get; set; }
    
    /// <summary>
    /// Time period start
    /// </summary>
    public DateTime PeriodStart { get; set; }
    
    /// <summary>
    /// Time period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }
    
    /// <summary>
    /// Duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }
    
    /// <summary>
    /// Additional metadata JSON
    /// </summary>
    public string? Metadata { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cost optimization recommendations
/// </summary>
public class CostOptimizationRecommendation
{
    public int Id { get; set; }
    
    /// <summary>
    /// Recommendation type: "ScheduleOptimization", "ResourceRightSizing", "IdleResource", "CostReduction"
    /// </summary>
    public string RecommendationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Resource type affected
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Resource identifier
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the recommendation
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Estimated cost savings
    /// </summary>
    public decimal EstimatedSavings { get; set; }
    
    /// <summary>
    /// Priority: "Low", "Medium", "High", "Critical"
    /// </summary>
    public string Priority { get; set; } = "Medium";
    
    /// <summary>
    /// Whether the recommendation has been implemented
    /// </summary>
    public bool IsImplemented { get; set; } = false;
    
    /// <summary>
    /// When it was implemented
    /// </summary>
    public DateTime? ImplementedAt { get; set; }
    
    /// <summary>
    /// Who implemented it
    /// </summary>
    public string? ImplementedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cost budget configuration
/// </summary>
public class CostBudget
{
    public int Id { get; set; }
    
    /// <summary>
    /// Budget name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Budget period: "Daily", "Weekly", "Monthly", "Yearly"
    /// </summary>
    public string Period { get; set; } = "Monthly";
    
    /// <summary>
    /// Budget amount
    /// </summary>
    public decimal BudgetAmount { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Resource type filter (null = all resources)
    /// </summary>
    public string? ResourceType { get; set; }
    
    /// <summary>
    /// Alert threshold percentage (e.g., 80 = alert at 80% of budget)
    /// </summary>
    public int AlertThresholdPercent { get; set; } = 80;
    
    /// <summary>
    /// Whether the budget is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

