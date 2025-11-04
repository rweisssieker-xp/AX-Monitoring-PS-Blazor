namespace AXMonitoringBU.Api.Models;

public class BatchJobHistory
{
    public string Caption { get; set; } = string.Empty;
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Reason { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    
    // Error Analysis Fields
    public string? ErrorCategory { get; set; }
    public string? ErrorSeverity { get; set; }
    public string? ErrorAnalysis { get; set; }
    public string? ErrorSuggestions { get; set; }
    public bool IsAnalyzed { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    
    // Helper property to determine if this is an error
    public bool IsError => !string.IsNullOrEmpty(Reason) && 
                          (Reason.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                           Reason.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                           Reason.Contains("Exception", StringComparison.OrdinalIgnoreCase));
}

