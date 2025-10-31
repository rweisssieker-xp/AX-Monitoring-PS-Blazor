using AX_Monitoring_Blazor.Core.Interfaces;
using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class MonitoringService : IMonitoringService
    {
        private readonly IBatchService _batchService;
        private readonly ISessionService _sessionService;
        private readonly IBlockingService _blockingService;
        private readonly ISqlHealthService _sqlHealthService;
        private readonly IAlertService _alertService;

        public MonitoringService(
            IBatchService batchService, 
            ISessionService sessionService, 
            IBlockingService blockingService, 
            ISqlHealthService sqlHealthService,
            IAlertService alertService)
        {
            _batchService = batchService;
            _sessionService = sessionService;
            _blockingService = blockingService;
            _sqlHealthService = sqlHealthService;
            _alertService = alertService;
        }

        public async Task<KpiDataDto> GetKpiDataAsync()
        {
            var batchStats = await _batchService.GetBatchJobStatisticsAsync();
            var sessionStats = await _sessionService.GetSessionStatisticsAsync();
            var blockingChains = await _blockingService.GetBlockingChainsAsync();
            var sqlHealth = await _sqlHealthService.GetSqlHealthMetricsAsync();

            var waitingJobs = Convert.ToInt32(batchStats["WaitingJobs"]);
            var runningJobs = Convert.ToInt32(batchStats["RunningJobs"]);
            var activeSessions = Convert.ToInt32(sessionStats["ActiveSessions"]);
            var errorRate = Convert.ToDouble(batchStats["ErrorRate"]);

            return new KpiDataDto
            {
                BatchBacklog = waitingJobs + runningJobs,
                ErrorRate = errorRate,
                ActiveSessions = activeSessions,
                BlockingChains = blockingChains.Count,
                CPUUsage = sqlHealth.CPUUsage,
                MemoryUsage = sqlHealth.MemoryUsage,
                ActiveConnections = sqlHealth.ActiveConnections,
                LongestQueryMinutes = sqlHealth.LongestQueryMinutes,
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task<List<BatchJobDto>> GetBatchJobsAsync(string status = "All")
        {
            return await _batchService.GetBatchJobsAsync(status);
        }

        public async Task<List<SessionDto>> GetSessionsAsync(bool activeOnly = false)
        {
            return await _sessionService.GetSessionsAsync(activeOnly);
        }

        public async Task<List<BlockingChainDto>> GetBlockingChainsAsync()
        {
            return await _blockingService.GetBlockingChainsAsync();
        }

        public async Task<SqlHealthDto> GetSqlHealthMetricsAsync()
        {
            return await _sqlHealthService.GetSqlHealthMetricsAsync();
        }

        public async Task<List<AlertDto>> GetAlertsAsync()
        {
            return await _alertService.GetAlertsAsync();
        }

        public async Task<bool> AcknowledgeAlertAsync(long alertId)
        {
            return await _alertService.AcknowledgeAlertAsync(alertId);
        }
    }
}