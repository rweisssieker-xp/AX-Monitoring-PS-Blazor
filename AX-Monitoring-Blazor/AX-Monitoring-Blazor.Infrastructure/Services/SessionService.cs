using AX_Monitoring_Blazor.Core.Interfaces;
using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class SessionService : ISessionService
    {
        private readonly ILogger<SessionService> _logger;

        public SessionService(ILogger<SessionService> logger)
        {
            _logger = logger;
        }

        public async Task<List<SessionDto>> GetSessionsAsync(bool activeOnly = false)
        {
            _logger.LogInformation($"Getting sessions, active only: {activeOnly}");
            
            // Simulate database query with mock data
            var mockSessions = new List<SessionDto>
            {
                new SessionDto
                {
                    SessionId = "1001",
                    UserId = "admin",
                    AOSServer = "AOS01",
                    LoginTime = DateTime.UtcNow.AddHours(-2),
                    LastActivity = DateTime.UtcNow.AddMinutes(-5),
                    IdleMinutes = 5,
                    Status = "Active",
                    ClientComputer = "CLIENT001",
                    ClientType = "WindowsClient"
                },
                new SessionDto
                {
                    SessionId = "1002",
                    UserId = "user1",
                    AOSServer = "AOS01",
                    LoginTime = DateTime.UtcNow.AddHours(-3),
                    LastActivity = DateTime.UtcNow.AddMinutes(-45),
                    IdleMinutes = 45,
                    Status = "Idle",
                    ClientComputer = "CLIENT002",
                    ClientType = "WindowsClient"
                },
                new SessionDto
                {
                    SessionId = "1003",
                    UserId = "user2",
                    AOSServer = "AOS02",
                    LoginTime = DateTime.UtcNow.AddHours(-5),
                    LastActivity = DateTime.UtcNow.AddHours(-4),
                    IdleMinutes = 60,
                    Status = "Inactive",
                    ClientComputer = "CLIENT003",
                    ClientType = "WebBrowser"
                }
            };

            if (activeOnly)
            {
                mockSessions = mockSessions.Where(s => s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            await Task.Delay(10); // Simulate async operation
            return mockSessions;
        }

        public async Task<Dictionary<string, object>> GetSessionStatisticsAsync()
        {
            _logger.LogInformation("Getting session statistics");
            
            var sessions = await GetSessionsAsync();
            
            var stats = new Dictionary<string, object>
            {
                { "TotalSessions", sessions.Count },
                { "ActiveSessions", sessions.Count(s => s.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)) },
                { "IdleSessions", sessions.Count(s => s.Status.Equals("Idle", StringComparison.OrdinalIgnoreCase)) },
                { "InactiveSessions", sessions.Count(s => s.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase)) },
                { "UniqueUsers", sessions.Select(s => s.UserId).Distinct().Count() },
                { "SessionsByAOS", sessions.GroupBy(s => s.AOSServer).ToDictionary(g => g.Key, g => g.Count()) }
            };

            await Task.Delay(10); // Simulate async operation
            return stats;
        }
    }
}