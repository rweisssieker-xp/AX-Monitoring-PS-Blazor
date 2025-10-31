using AX_Monitoring_Blazor.Core.Interfaces;
using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class BlockingService : IBlockingService
    {
        private readonly ILogger<BlockingService> _logger;

        public BlockingService(ILogger<BlockingService> logger)
        {
            _logger = logger;
        }

        public async Task<List<BlockingChainDto>> GetBlockingChainsAsync()
        {
            _logger.LogInformation("Getting blocking chains");
            
            // Simulate database query with mock data
            var mockBlockingChains = new List<BlockingChainDto>
            {
                new BlockingChainDto
                {
                    BlockingSession = 54,
                    BlockedSession = 102,
                    WaitType = "PAGEIOLATCH_EX",
                    Resource = "2:1:12345",
                    DurationSeconds = 120,
                    Command = "SELECT",
                    Status = "suspended",
                    DatabaseName = "AXDB",
                    SQLText = "SELECT * FROM INVENTTABLE WITH(NOLOCK)"
                },
                new BlockingChainDto
                {
                    BlockingSession = 67,
                    BlockedSession = 205,
                    WaitType = "LCK_M_X",
                    Resource = "KEY: 7:72057594046152704",
                    DurationSeconds = 45,
                    Command = "UPDATE",
                    Status = "suspended",
                    DatabaseName = "AXDB",
                    SQLText = "UPDATE INVENTTABLE SET ..."
                }
            };

            await Task.Delay(10); // Simulate async operation
            return mockBlockingChains;
        }
    }
}