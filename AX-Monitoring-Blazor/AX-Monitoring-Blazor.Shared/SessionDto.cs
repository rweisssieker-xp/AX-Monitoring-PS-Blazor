namespace AX_Monitoring_Blazor.Shared
{
    public class SessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AOSServer { get; set; } = string.Empty;
        public DateTime? LoginTime { get; set; }
        public DateTime? LastActivity { get; set; }
        public double IdleMinutes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ClientComputer { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty;
    }
}