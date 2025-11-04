namespace AXMonitoringBU.Api.Models;

public class SqlHealth
{
    public int Id { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double IoWait { get; set; }
    public double TempDbUsage { get; set; }
    public int ActiveConnections { get; set; }
    public int LongestRunningQueryMinutes { get; set; }
    public DateTime RecordedAt { get; set; }
}

