namespace AX_Monitoring_Blazor.Shared
{
    public class BlockingChainDto
    {
        public int BlockingSession { get; set; }
        public int BlockedSession { get; set; }
        public string WaitType { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string Command { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string SQLText { get; set; } = string.Empty;
    }
}