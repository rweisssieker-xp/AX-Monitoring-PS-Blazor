using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IBlockingService
{
    Task<IEnumerable<BlockingChain>> GetBlockingChainsAsync(bool activeOnly = true);
    Task<BlockingChain?> GetBlockingChainByIdAsync(int id);
}

public class BlockingService : IBlockingService
{
    private readonly AXDbContext _context;
    private readonly ILogger<BlockingService> _logger;

    public BlockingService(AXDbContext context, ILogger<BlockingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<BlockingChain>> GetBlockingChainsAsync(bool activeOnly = true)
    {
        try
        {
            var query = _context.BlockingChains.AsQueryable();

            if (activeOnly)
            {
                query = query.Where(b => b.ResolvedAt == null);
            }

            return await query
                .OrderByDescending(b => b.DurationSeconds)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocking chains");
            throw;
        }
    }

    public async Task<BlockingChain?> GetBlockingChainByIdAsync(int id)
    {
        try
        {
            return await _context.BlockingChains.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocking chain by id {BlockingChainId}", id);
            throw;
        }
    }
}

