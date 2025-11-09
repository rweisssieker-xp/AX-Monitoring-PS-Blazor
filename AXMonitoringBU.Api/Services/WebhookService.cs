using System.Net.Http.Json;
using System.Text.Json;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AXMonitoringBU.Api.Services;

public interface IWebhookService
{
    Task<bool> SendWebhookAsync(string url, object payload, string? eventType = null);
    Task<bool> SendAlertWebhookAsync(string webhookUrl, Alert alert);
    Task<bool> SendMetricWebhookAsync(string webhookUrl, Dictionary<string, object> metrics);
    Task<List<WebhookSubscription>> GetSubscriptionsAsync();
    Task<WebhookSubscription> CreateSubscriptionAsync(WebhookSubscription subscription);
    Task<bool> DeleteSubscriptionAsync(int id);
}

public class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AXDbContext _context;

    public WebhookService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger,
        IConfiguration configuration,
        AXDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }

    public async Task<bool> SendWebhookAsync(string url, object payload, string? eventType = null)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Webhook sent successfully to {Url} for event {EventType}", url, eventType ?? "unknown");
                return true;
            }
            else
            {
                _logger.LogWarning("Webhook failed with status {StatusCode} for {Url}", response.StatusCode, url);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to {Url}", url);
            return false;
        }
    }

    public async Task<bool> SendAlertWebhookAsync(string webhookUrl, Alert alert)
    {
        var payload = new
        {
            EventType = "alert",
            Timestamp = DateTime.UtcNow,
            Alert = new
            {
                alert.Id,
                alert.AlertId,
                alert.Type,
                alert.Severity,
                alert.Message,
                alert.Status,
                alert.Timestamp,
                alert.ResolvedAt
            }
        };

        return await SendWebhookAsync(webhookUrl, payload, "alert");
    }

    public async Task<bool> SendMetricWebhookAsync(string webhookUrl, Dictionary<string, object> metrics)
    {
        var payload = new
        {
            EventType = "metric",
            Timestamp = DateTime.UtcNow,
            Metrics = metrics
        };

        return await SendWebhookAsync(webhookUrl, payload, "metric");
    }

    public async Task<List<WebhookSubscription>> GetSubscriptionsAsync()
    {
        try
        {
            return await _context.WebhookSubscriptions
                .Where(s => s.Enabled)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook subscriptions");
            return new List<WebhookSubscription>();
        }
    }

    public async Task<WebhookSubscription> CreateSubscriptionAsync(WebhookSubscription subscription)
    {
        try
        {
            subscription.CreatedAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;
            
            _context.WebhookSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created webhook subscription: {Name} -> {Url}", subscription.Name, subscription.Url);
            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook subscription: {Name}", subscription.Name);
            throw;
        }
    }

    public async Task<bool> DeleteSubscriptionAsync(int id)
    {
        try
        {
            var subscription = await _context.WebhookSubscriptions.FindAsync(id);
            if (subscription != null)
            {
                _context.WebhookSubscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted webhook subscription: {Name}", subscription.Name);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook subscription: {Id}", id);
            return false;
        }
    }
}

