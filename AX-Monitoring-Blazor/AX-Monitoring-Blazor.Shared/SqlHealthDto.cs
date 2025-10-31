namespace AX_Monitoring_Blazor.Shared
{
    public class SqlHealthDto
    {
        public double CPUUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public int LongestQueryMinutes { get; set; }
        public List<WaitStatDto> TopWaits { get; set; } = new List<WaitStatDto>();
    }
    
    public class WaitStatDto
    {
        public string WaitType { get; set; } = string.Empty;
        public double WaitTimeSeconds { get; set; }
        public int WaitingTasks { get; set; }
    }
}