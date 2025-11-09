using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IWebhookService webhookService,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions()
    {
        try
        {
            var subscriptions = await _webhookService.GetSubscriptionsAsync();
            return Ok(new
            {
                subscriptions = subscriptions,
                count = subscriptions.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook subscriptions");
            return StatusCode(500, new { error = "Failed to retrieve webhook subscriptions" });
        }
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateWebhookSubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Url))
            {
                return BadRequest(new { error = "Name and Url are required" });
            }

            var subscription = new WebhookSubscription
            {
                Name = request.Name,
                Url = request.Url,
                EventType = request.EventType ?? "all",
                Secret = request.Secret,
                Enabled = request.Enabled ?? true
            };

            var created = await _webhookService.CreateSubscriptionAsync(subscription);
            return Ok(new
            {
                subscription = created,
                message = "Webhook subscription created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook subscription");
            return StatusCode(500, new { error = "Failed to create webhook subscription" });
        }
    }

    [HttpDelete("subscriptions/{id}")]
    public async Task<IActionResult> DeleteSubscription(int id)
    {
        try
        {
            var deleted = await _webhookService.DeleteSubscriptionAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = "Webhook subscription not found" });
            }

            return Ok(new { message = "Webhook subscription deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook subscription");
            return StatusCode(500, new { error = "Failed to delete webhook subscription" });
        }
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestWebhook([FromBody] TestWebhookRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Url))
            {
                return BadRequest(new { error = "Url is required" });
            }

            var testPayload = new
            {
                EventType = "test",
                Timestamp = DateTime.UtcNow,
                Message = "This is a test webhook from AX Monitoring BU"
            };

            var success = await _webhookService.SendWebhookAsync(request.Url, testPayload, "test");
            
            if (success)
            {
                return Ok(new { message = "Test webhook sent successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to send test webhook" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing webhook");
            return StatusCode(500, new { error = "Failed to test webhook" });
        }
    }
}

public class CreateWebhookSubscriptionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? EventType { get; set; }
    public string? Secret { get; set; }
    public bool? Enabled { get; set; }
}

public class TestWebhookRequest
{
    public string Url { get; set; } = string.Empty;
}

