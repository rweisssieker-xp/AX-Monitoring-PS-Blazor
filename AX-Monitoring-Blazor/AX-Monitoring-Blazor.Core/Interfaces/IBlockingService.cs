using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IBlockingService
    {
        Task<List<BlockingChainDto>> GetBlockingChainsAsync();
    }
}