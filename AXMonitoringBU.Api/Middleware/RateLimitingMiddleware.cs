using System.Collections.Concurrent;
using System.Net;

namespace AXMonitoringBU.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitCache = new();
    private readonly int _maxRequestsPerMinute;
    private readonly int _maxRequestsPerHour;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _maxRequestsPerMinute = int.Parse(configuration["Api:RateLimit:RequestsPerMinute"] ?? "60");
        _maxRequestsPerHour = int.Parse(configuration["Api:RateLimit:RequestsPerHour"] ?? "1000");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var now = DateTime.UtcNow;

        // Clean up old entries periodically
        if (_rateLimitCache.Count > 10000)
        {
            CleanupOldEntries(now);
        }

        var rateLimitInfo = _rateLimitCache.GetOrAdd(clientId, _ => new RateLimitInfo
        {
            ClientId = clientId,
            FirstRequest = now,
            Requests = new List<DateTime>()
        });

        var shouldThrottle = false;
        var retryAfterSeconds = 0;

        lock (rateLimitInfo)
        {
            // Remove requests older than 1 hour
            rateLimitInfo.Requests.RemoveAll(r => r < now.AddHours(-1));

            // Check per-minute limit
            var requestsLastMinute = rateLimitInfo.Requests.Count(r => r > now.AddMinutes(-1));
            if (requestsLastMinute >= _maxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}: {Count} requests in last minute", 
                    clientId, requestsLastMinute);
                shouldThrottle = true;
                retryAfterSeconds = 60;
            }

            // Check per-hour limit
            var requestsLastHour = rateLimitInfo.Requests.Count;
            if (!shouldThrottle && requestsLastHour >= _maxRequestsPerHour)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}: {Count} requests in last hour", 
                    clientId, requestsLastHour);
                shouldThrottle = true;
                retryAfterSeconds = 3600;
            }

            if (!shouldThrottle)
            {
                // Add current request when within limits
                rateLimitInfo.Requests.Add(now);
            }
        }

        if (shouldThrottle)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit-Minute"] = _maxRequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Limit-Hour"] = _maxRequestsPerHour.ToString();

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // Try to get client IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // If authenticated, use user identity
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return $"{context.User.Identity.Name}@{ipAddress}";
        }

        return ipAddress;
    }

    private void CleanupOldEntries(DateTime now)
    {
        var keysToRemove = _rateLimitCache
            .Where(kvp => kvp.Value.Requests.All(r => r < now.AddHours(-1)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimitCache.TryRemove(key, out _);
        }
    }
}

public class RateLimitInfo
{
    public string ClientId { get; set; } = string.Empty;
    public DateTime FirstRequest { get; set; }
    public List<DateTime> Requests { get; set; } = new();
}

