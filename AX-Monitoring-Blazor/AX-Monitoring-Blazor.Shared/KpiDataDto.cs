namespace AX_Monitoring_Blazor.Shared
{
    public class KpiDataDto
    {
        public int BatchBacklog { get; set; }
        public double ErrorRate { get; set; }
        public int ActiveSessions { get; set; }
        public int BlockingChains { get; set; }
        public double CPUUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public int LongestQueryMinutes { get; set; }
        public DateTime Timestamp { get; set; }
    }
}