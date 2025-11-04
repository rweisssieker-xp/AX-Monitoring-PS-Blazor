namespace AXMonitoringBU.Api.Models;

public class BatchJobHistoryAnalysis
{
    public int Id { get; set; }
    public string Caption { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
    public string ErrorReason { get; set; } = string.Empty;
    
    // Analysis Results
    public string? ErrorCategory { get; set; }
    public string? ErrorSeverity { get; set; }
    public string? ErrorAnalysis { get; set; }
    public string? ErrorSuggestions { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    
    // Create a unique key for matching
    public string UniqueKey => $"{Caption}_{CreatedDateTime:yyyy-MM-dd_HH:mm:ss}";
}

