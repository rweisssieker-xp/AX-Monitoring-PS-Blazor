using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IPerformanceBudgetService
{
    Task<PerformanceBudgetResult> CheckPerformanceBudgetAsync(string endpoint, TimeSpan duration);
    Task<List<PerformanceBudget>> GetPerformanceBudgetsAsync();
    Task<PerformanceBudget> SetPerformanceBudgetAsync(string endpoint, double p95ThresholdMs);
    Task<bool> IsWithinBudgetAsync(string endpoint, TimeSpan duration);
}

public class PerformanceBudgetResult
{
    public string Endpoint { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public double P95ThresholdMs { get; set; }
    public bool IsWithinBudget { get; set; }
    public double OverBudgetPercent { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PerformanceBudgetService : IPerformanceBudgetService
{
    private readonly AXDbContext _context;
    private readonly ILogger<PerformanceBudgetService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, PerformanceBudget> _budgetCache = new();
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);

    public PerformanceBudgetService(
        AXDbContext context,
        ILogger<PerformanceBudgetService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<PerformanceBudgetResult> CheckPerformanceBudgetAsync(string endpoint, TimeSpan duration)
    {
        try
        {
            var budget = await GetBudgetForEndpointAsync(endpoint);
            var durationMs = duration.TotalMilliseconds;
            var thresholdMs = budget?.P95ThresholdMs ?? GetDefaultThreshold(endpoint);
            
            var isWithinBudget = durationMs <= thresholdMs;
            var overBudgetPercent = isWithinBudget ? 0 : ((durationMs - thresholdMs) / thresholdMs) * 100;

            if (!isWithinBudget)
            {
                _logger.LogWarning("Performance budget exceeded for {Endpoint}: {DurationMs}ms > {ThresholdMs}ms ({OverBudget}% over)", 
                    endpoint, durationMs, thresholdMs, overBudgetPercent);
            }

            return new PerformanceBudgetResult
            {
                Endpoint = endpoint,
                DurationMs = durationMs,
                P95ThresholdMs = thresholdMs,
                IsWithinBudget = isWithinBudget,
                OverBudgetPercent = overBudgetPercent,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking performance budget for {Endpoint}", endpoint);
            // Return a permissive result on error
            return new PerformanceBudgetResult
            {
                Endpoint = endpoint,
                DurationMs = duration.TotalMilliseconds,
                P95ThresholdMs = 3000, // Default 3s
                IsWithinBudget = true,
                OverBudgetPercent = 0,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<List<PerformanceBudget>> GetPerformanceBudgetsAsync()
    {
        try
        {
            return await _context.Set<PerformanceBudget>()
                .OrderBy(b => b.Endpoint)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance budgets");
            throw;
        }
    }

    public async Task<PerformanceBudget> SetPerformanceBudgetAsync(string endpoint, double p95ThresholdMs)
    {
        try
        {
            var existing = await _context.Set<PerformanceBudget>()
                .FirstOrDefaultAsync(b => b.Endpoint == endpoint);

            if (existing != null)
            {
                existing.P95ThresholdMs = p95ThresholdMs;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.Set<PerformanceBudget>().Update(existing);
                await _context.SaveChangesAsync();
                InvalidateCache();
                return existing;
            }
            else
            {
                var budget = new PerformanceBudget
                {
                    Endpoint = endpoint,
                    P95ThresholdMs = p95ThresholdMs,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Set<PerformanceBudget>().Add(budget);
                await _context.SaveChangesAsync();
                InvalidateCache();
                return budget;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting performance budget for {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> IsWithinBudgetAsync(string endpoint, TimeSpan duration)
    {
        var result = await CheckPerformanceBudgetAsync(endpoint, duration);
        return result.IsWithinBudget;
    }

    private async Task<PerformanceBudget?> GetBudgetForEndpointAsync(string endpoint)
    {
        // Check cache first
        if (DateTime.UtcNow < _cacheExpiry && _budgetCache.TryGetValue(endpoint, out var cachedBudget))
        {
            return cachedBudget;
        }

        try
        {
            var budget = await _context.Set<PerformanceBudget>()
                .FirstOrDefaultAsync(b => b.Endpoint == endpoint);

            if (budget != null)
            {
                _budgetCache[endpoint] = budget;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheTTL);
            }

            return budget;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting budget for endpoint {Endpoint}, using default", endpoint);
            return null;
        }
    }

    private double GetDefaultThreshold(string endpoint)
    {
        // Default thresholds based on endpoint patterns
        if (endpoint.Contains("overview", StringComparison.OrdinalIgnoreCase) ||
            endpoint.Contains("dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return 3000; // 3 seconds for overview/dashboard
        }
        
        if (endpoint.Contains("api/metrics", StringComparison.OrdinalIgnoreCase))
        {
            return 2000; // 2 seconds for metrics
        }

        return 1000; // 1 second default
    }

    private void InvalidateCache()
    {
        _budgetCache.Clear();
        _cacheExpiry = DateTime.MinValue;
    }
}

