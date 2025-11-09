namespace AXMonitoringBU.Api.Models;

/// <summary>
/// Represents a shared dashboard that can be accessed by multiple users
/// </summary>
public class SharedDashboard
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier for the dashboard
    /// </summary>
    public string DashboardId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the dashboard
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the dashboard
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Dashboard layout JSON
    /// </summary>
    public string LayoutJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Owner/Creator of the dashboard
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the dashboard is public (accessible to all users)
    /// </summary>
    public bool IsPublic { get; set; } = false;
    
    /// <summary>
    /// Whether the dashboard is a team workspace dashboard
    /// </summary>
    public bool IsTeamWorkspace { get; set; } = false;
    
    /// <summary>
    /// Team/Workspace name (if IsTeamWorkspace is true)
    /// </summary>
    public string? TeamName { get; set; }
    
    /// <summary>
    /// Whether the dashboard is the default dashboard
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Tags for categorization
    /// </summary>
    public string? Tags { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>
    /// Number of times the dashboard was accessed
    /// </summary>
    public int AccessCount { get; set; } = 0;
}

/// <summary>
/// Represents dashboard sharing permissions
/// </summary>
public class DashboardShare
{
    public int Id { get; set; }
    public int DashboardId { get; set; }
    
    /// <summary>
    /// Username or email of the user with access
    /// </summary>
    public string SharedWith { get; set; } = string.Empty;
    
    /// <summary>
    /// Permission level: "view", "edit", "admin"
    /// </summary>
    public string Permission { get; set; } = "view";
    
    /// <summary>
    /// Who shared the dashboard
    /// </summary>
    public string SharedBy { get; set; } = string.Empty;
    
    public DateTime SharedAt { get; set; } = DateTime.UtcNow;
}

