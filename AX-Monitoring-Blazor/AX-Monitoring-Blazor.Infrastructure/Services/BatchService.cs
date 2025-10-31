using AX_Monitoring_Blazor.Core.Interfaces;
using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class BatchService : IBatchService
    {
        private readonly ILogger<BatchService> _logger;

        public BatchService(ILogger<BatchService> logger)
        {
            _logger = logger;
        }

        public async Task<List<BatchJobDto>> GetBatchJobsAsync(string status = "All")
        {
            // This is a placeholder implementation
            // In a real implementation, this would query the database
            _logger.LogInformation($"Getting batch jobs with status filter: {status}");
            
            // Simulate database query with mock data
            var mockBatchJobs = new List<BatchJobDto>
            {
                new BatchJobDto
                {
                    Id = "1",
                    Name = "Financial Reports",
                    Status = "Running",
                    StartTime = DateTime.UtcNow.AddMinutes(-15),
                    EndTime = null,
                    DurationMinutes = 15.0,
                    AOSServer = "AOS01",
                    Progress = 75,
                    Company = "ceu",
                    CreatedBy = "System"
                },
                new BatchJobDto
                {
                    Id = "2",
                    Name = "Data Import",
                    Status = "Waiting",
                    StartTime = DateTime.UtcNow.AddMinutes(-5),
                    EndTime = null,
                    DurationMinutes = 5.0,
                    AOSServer = "AOS02",
                    Progress = 0,
                    Company = "ceu",
                    CreatedBy = "admin"
                },
                new BatchJobDto
                {
                    Id = "3",
                    Name = "Inventory Update",
                    Status = "Completed",
                    StartTime = DateTime.UtcNow.AddHours(-1),
                    EndTime = DateTime.UtcNow.AddMinutes(-45),
                    DurationMinutes = 15.0,
                    AOSServer = "AOS01",
                    Progress = 100,
                    Company = "ceu",
                    CreatedBy = "System"
                }
            };

            if (status != "All")
            {
                mockBatchJobs = mockBatchJobs.Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            await Task.Delay(10); // Simulate async operation
            return mockBatchJobs;
        }

        public async Task<Dictionary<string, object>> GetBatchJobStatisticsAsync(int lastHours = 24)
        {
            _logger.LogInformation($"Getting batch job statistics for last {lastHours} hours");
            
            // Simulate statistics calculation
            var batchJobs = await GetBatchJobsAsync();
            var runningJobs = batchJobs.Count(b => b.Status.Equals("Running", StringComparison.OrdinalIgnoreCase));
            var waitingJobs = batchJobs.Count(b => b.Status.Equals("Waiting", StringComparison.OrdinalIgnoreCase));
            var completedJobs = batchJobs.Count(b => b.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
            var errorJobs = batchJobs.Count(b => b.Status.Equals("Error", StringComparison.OrdinalIgnoreCase));
            var totalJobs = batchJobs.Count;

            var stats = new Dictionary<string, object>
            {
                { "TotalJobs", totalJobs },
                { "RunningJobs", runningJobs },
                { "WaitingJobs", waitingJobs },
                { "ErrorJobs", errorJobs },
                { "CompletedJobs", completedJobs },
                { "AvgDurationMinutes", 12.5 },
                { "ErrorRate", totalJobs > 0 ? Math.Round((double)errorJobs / totalJobs * 100, 2) : 0.0 }
            };

            await Task.Delay(10); // Simulate async operation
            return stats;
        }
    }
}