using AX_Monitoring_Blazor.Core.Interfaces;
using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class SqlHealthService : ISqlHealthService
    {
        private readonly ILogger<SqlHealthService> _logger;

        public SqlHealthService(ILogger<SqlHealthService> logger)
        {
            _logger = logger;
        }

        public async Task<SqlHealthDto> GetSqlHealthMetricsAsync()
        {
            _logger.LogInformation("Getting SQL health metrics");
            
            // Simulate metrics retrieval with mock data
            var mockMetrics = new SqlHealthDto
            {
                CPUUsage = 45.6,
                MemoryUsage = 78.2,
                ActiveConnections = 42,
                LongestQueryMinutes = 8,
                TopWaits = new List<WaitStatDto>
                {
                    new WaitStatDto { WaitType = "PAGEIOLATCH_SH", WaitTimeSeconds = 125.4, WaitingTasks = 12 },
                    new WaitStatDto { WaitType = "SOS_SCHEDULER_YIELD", WaitTimeSeconds = 89.2, WaitingTasks = 34 },
                    new WaitStatDto { WaitType = "ASYNC_NETWORK_IO", WaitTimeSeconds = 67.8, WaitingTasks = 8 },
                    new WaitStatDto { WaitType = "PAGEIOLATCH_EX", WaitTimeSeconds = 54.3, WaitingTasks = 5 },
                    new WaitStatDto { WaitType = "LCK_M_S", WaitTimeSeconds = 43.2, WaitingTasks = 15 }
                }
            };

            await Task.Delay(10); // Simulate async operation
            return mockMetrics;
        }

        public async Task<List<Dictionary<string, object>>> GetDatabaseSizeAsync()
        {
            _logger.LogInformation("Getting database size information");
            
            // Simulate database size query with mock data
            var mockDatabaseSizes = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "DatabaseName", "AXDB" },
                    { "UsedSpaceMB", 1250.5 },
                    { "TotalSpaceMB", 2048.0 },
                    { "FreeSpaceMB", 797.5 },
                    { "UsagePercent", 61.1 }
                },
                new Dictionary<string, object>
                {
                    { "DatabaseName", "Model" },
                    { "UsedSpaceMB", 12.3 },
                    { "TotalSpaceMB", 100.0 },
                    { "FreeSpaceMB", 87.7 },
                    { "UsagePercent", 12.3 }
                }
            };

            await Task.Delay(10); // Simulate async operation
            return mockDatabaseSizes;
        }
    }
}