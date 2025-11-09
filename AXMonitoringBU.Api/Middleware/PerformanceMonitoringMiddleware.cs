using System.Diagnostics;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Middleware;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = $"{context.Request.Method} {context.Request.Path}";

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;

            // Log performance
            if (duration.TotalMilliseconds > 1000)
            {
                _logger.LogWarning("Slow request detected: {Endpoint} took {Duration}ms", 
                    endpoint, duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug("Request completed: {Endpoint} took {Duration}ms", 
                    endpoint, duration.TotalMilliseconds);
            }

            // Check performance budget - resolve service from request scope
            var performanceBudgetService = context.RequestServices.GetService<IPerformanceBudgetService>();
            if (performanceBudgetService != null)
            {
                try
                {
                    var budgetResult = await performanceBudgetService.CheckPerformanceBudgetAsync(endpoint, duration);
                    if (!budgetResult.IsWithinBudget)
                    {
                        _logger.LogWarning("Performance budget exceeded for {Endpoint}: {Duration}ms > {Threshold}ms ({OverBudget}% over)", 
                            endpoint, duration.TotalMilliseconds, budgetResult.P95ThresholdMs, budgetResult.OverBudgetPercent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking performance budget");
                }
            }

            // Add performance headers
            context.Response.Headers["X-Response-Time-Ms"] = duration.TotalMilliseconds.ToString("F2");
        }
    }
}

