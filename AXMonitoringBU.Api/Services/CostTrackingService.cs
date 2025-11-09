using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;
using System.Text.Json;

namespace AXMonitoringBU.Api.Services;

public interface ICostTrackingService
{
    Task<IEnumerable<CostTracking>> GetCostsAsync(string? resourceType = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<decimal> GetTotalCostAsync(string? resourceType = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<CostTracking>> GetCostsByResourceAsync(string resourceType, string resourceId);
    Task<CostTracking> RecordCostAsync(CostTracking cost);
    Task<IEnumerable<CostOptimizationRecommendation>> GetRecommendationsAsync(bool? implemented = null, string? priority = null);
    Task<CostOptimizationRecommendation> CreateRecommendationAsync(CostOptimizationRecommendation recommendation);
    Task<bool> ImplementRecommendationAsync(int recommendationId, string implementedBy);
    Task<IEnumerable<CostBudget>> GetBudgetsAsync(bool? active = null);
    Task<CostBudget> CreateBudgetAsync(CostBudget budget);
    Task<bool> UpdateBudgetAsync(int id, CostBudget budget);
    Task<bool> CheckBudgetAlertsAsync();
    Task<Dictionary<string, decimal>> GetCostBreakdownAsync(DateTime? startDate = null, DateTime? endDate = null);
}

public class CostTrackingService : ICostTrackingService
{
    private readonly AXDbContext _context;
    private readonly ILogger<CostTrackingService> _logger;
    private readonly IAlertService? _alertService;

    public CostTrackingService(
        AXDbContext context,
        ILogger<CostTrackingService> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _alertService = serviceProvider.GetService<IAlertService>();
    }

    public async Task<IEnumerable<CostTracking>> GetCostsAsync(string? resourceType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.CostTrackings.AsQueryable();

            if (!string.IsNullOrEmpty(resourceType))
            {
                query = query.Where(c => c.ResourceType == resourceType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(c => c.PeriodStart >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(c => c.PeriodEnd <= endDate.Value);
            }

            return await query
                .OrderByDescending(c => c.PeriodStart)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting costs");
            throw;
        }
    }

    public async Task<decimal> GetTotalCostAsync(string? resourceType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var costs = await GetCostsAsync(resourceType, startDate, endDate);
            return costs.Sum(c => c.Cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total cost");
            throw;
        }
    }

    public async Task<IEnumerable<CostTracking>> GetCostsByResourceAsync(string resourceType, string resourceId)
    {
        try
        {
            return await _context.CostTrackings
                .Where(c => c.ResourceType == resourceType && c.ResourceId == resourceId)
                .OrderByDescending(c => c.PeriodStart)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting costs for resource {ResourceType}/{ResourceId}", resourceType, resourceId);
            throw;
        }
    }

    public async Task<CostTracking> RecordCostAsync(CostTracking cost)
    {
        try
        {
            cost.CreatedAt = DateTime.UtcNow;
            _context.CostTrackings.Add(cost);
            await _context.SaveChangesAsync();
            return cost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording cost");
            throw;
        }
    }

    public async Task<IEnumerable<CostOptimizationRecommendation>> GetRecommendationsAsync(bool? implemented = null, string? priority = null)
    {
        try
        {
            var query = _context.CostOptimizationRecommendations.AsQueryable();

            if (implemented.HasValue)
            {
                query = query.Where(r => r.IsImplemented == implemented.Value);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(r => r.Priority == priority);
            }

            return await query
                .OrderByDescending(r => r.Priority == "Critical" ? 4 : r.Priority == "High" ? 3 : r.Priority == "Medium" ? 2 : 1)
                .ThenByDescending(r => r.EstimatedSavings)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            throw;
        }
    }

    public async Task<CostOptimizationRecommendation> CreateRecommendationAsync(CostOptimizationRecommendation recommendation)
    {
        try
        {
            recommendation.CreatedAt = DateTime.UtcNow;
            _context.CostOptimizationRecommendations.Add(recommendation);
            await _context.SaveChangesAsync();
            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recommendation");
            throw;
        }
    }

    public async Task<bool> ImplementRecommendationAsync(int recommendationId, string implementedBy)
    {
        try
        {
            var recommendation = await _context.CostOptimizationRecommendations.FindAsync(recommendationId);
            if (recommendation == null)
            {
                return false;
            }

            recommendation.IsImplemented = true;
            recommendation.ImplementedAt = DateTime.UtcNow;
            recommendation.ImplementedBy = implementedBy;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error implementing recommendation {RecommendationId}", recommendationId);
            throw;
        }
    }

    public async Task<IEnumerable<CostBudget>> GetBudgetsAsync(bool? active = null)
    {
        try
        {
            var query = _context.CostBudgets.AsQueryable();

            if (active.HasValue)
            {
                query = query.Where(b => b.IsActive == active.Value);
            }

            return await query
                .OrderBy(b => b.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting budgets");
            throw;
        }
    }

    public async Task<CostBudget> CreateBudgetAsync(CostBudget budget)
    {
        try
        {
            budget.CreatedAt = DateTime.UtcNow;
            _context.CostBudgets.Add(budget);
            await _context.SaveChangesAsync();
            return budget;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating budget");
            throw;
        }
    }

    public async Task<bool> UpdateBudgetAsync(int id, CostBudget budget)
    {
        try
        {
            var existing = await _context.CostBudgets.FindAsync(id);
            if (existing == null)
            {
                return false;
            }

            existing.Name = budget.Name;
            existing.Period = budget.Period;
            existing.BudgetAmount = budget.BudgetAmount;
            existing.Currency = budget.Currency;
            existing.ResourceType = budget.ResourceType;
            existing.AlertThresholdPercent = budget.AlertThresholdPercent;
            existing.IsActive = budget.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating budget {BudgetId}", id);
            throw;
        }
    }

    public async Task<bool> CheckBudgetAlertsAsync()
    {
        try
        {
            var activeBudgets = await _context.CostBudgets
                .Where(b => b.IsActive)
                .ToListAsync();

            foreach (var budget in activeBudgets)
            {
                var (startDate, endDate) = GetBudgetPeriod(budget.Period);
                var totalCost = await GetTotalCostAsync(budget.ResourceType, startDate, endDate);
                var percentage = (double)(totalCost / budget.BudgetAmount * 100);

                if (percentage >= budget.AlertThresholdPercent)
                {
                    var alertMessage = $"Cost budget '{budget.Name}' is at {percentage:F1}% ({totalCost:F2} {budget.Currency} / {budget.BudgetAmount:F2} {budget.Currency})";
                    
                    if (_alertService != null)
                    {
                        var severity = percentage >= 100 ? "Critical" : percentage >= budget.AlertThresholdPercent ? "Warning" : "Info";
                        await _alertService.CreateAlertAsync(
                            "CostBudget",
                            severity,
                            alertMessage,
                            "CostTrackingService");
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking budget alerts");
            return false;
        }
    }

    public async Task<Dictionary<string, decimal>> GetCostBreakdownAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var costs = await GetCostsAsync(null, startDate, endDate);
            
            return costs
                .GroupBy(c => c.ResourceType)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Cost));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost breakdown");
            throw;
        }
    }

    private (DateTime startDate, DateTime endDate) GetBudgetPeriod(string period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            "Daily" => (now.Date, now.Date.AddDays(1)),
            "Weekly" => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(-(int)now.DayOfWeek).AddDays(7)),
            "Monthly" => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1)),
            "Yearly" => (new DateTime(now.Year, 1, 1), new DateTime(now.Year + 1, 1, 1)),
            _ => (now.Date.AddMonths(-1), now.Date)
        };
    }
}

