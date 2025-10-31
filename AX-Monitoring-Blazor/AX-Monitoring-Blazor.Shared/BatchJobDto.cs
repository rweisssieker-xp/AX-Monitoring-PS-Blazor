namespace AX_Monitoring_Blazor.Shared
{
    public class BatchJobDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public double DurationMinutes { get; set; }
        public string AOSServer { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Company { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }
}