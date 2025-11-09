namespace AXMonitoringBU.Api.Models;

public class ExportTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // "BatchJob", "Session", "Alert"
    public string Format { get; set; } = string.Empty; // "CSV", "Excel"
    public string FieldsJson { get; set; } = "[]"; // JSON array of field names
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Helper properties for JSON serialization
    public List<string> Fields
    {
        get => string.IsNullOrEmpty(FieldsJson) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(FieldsJson) ?? new List<string>();
        set => FieldsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}





