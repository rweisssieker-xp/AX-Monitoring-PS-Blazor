using System.Net.Http.Json;
using System.Text.Json;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface ITeamsNotificationService
{
    Task<bool> SendAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<bool> SendDigestAsync(IEnumerable<Alert> alerts, string period = "hourly", CancellationToken cancellationToken = default);
    Task<bool> SendEscalationMessageAsync(string recipients, string escalationMessage, CancellationToken cancellationToken = default);
}

public class TeamsNotificationService : ITeamsNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeamsNotificationService> _logger;

    public TeamsNotificationService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<TeamsNotificationService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendAlertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookUrl = GetWebhookUrl(alert.Severity);
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("No Teams webhook URL configured for severity {Severity}", alert.Severity);
                return false;
            }

            var card = CreateTeamsCard(alert);
            var payload = new
            {
                type = "message",
                attachments = new[] { card }
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams alert sent successfully for alert {AlertId}", alert.AlertId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send Teams alert. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams alert for {AlertId}", alert.AlertId);
            return false;
        }
    }

    public async Task<bool> SendDigestAsync(IEnumerable<Alert> alerts, string period = "hourly", CancellationToken cancellationToken = default)
    {
        try
        {
            var alertsList = alerts.ToList();
            if (!alertsList.Any())
            {
                return true;
            }

            var webhookUrl = GetWebhookUrl("Critical"); // Use critical webhook for digest
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("No Teams webhook URL configured for digest");
                return false;
            }

            var card = CreateDigestCard(alertsList, period);
            var payload = new
            {
                type = "message",
                attachments = new[] { card }
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams digest sent successfully with {Count} alerts", alertsList.Count);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send Teams digest. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams digest");
            return false;
        }
    }

    private string? GetWebhookUrl(string severity)
    {
        var configKey = severity switch
        {
            "Critical" => "Alerting:Teams:CriticalWebhookUrl",
            "Warning" => "Alerting:Teams:WarningWebhookUrl",
            _ => "Alerting:Teams:InfoWebhookUrl"
        };

        return _configuration[configKey];
    }

    private object CreateTeamsCard(Alert alert)
    {
        var color = alert.Severity switch
        {
            "Critical" => "FF0000",
            "Warning" => "FFA500",
            _ => "0080FF"
        };

        return new
        {
            contentType = "application/vnd.microsoft.card.adaptive",
            content = new
            {
                type = "AdaptiveCard",
                version = "1.4",
                body = new object[]
                {
                    new
                    {
                        type = "TextBlock",
                        text = $"AX Monitor Alert: {alert.Type}",
                        weight = "Bolder",
                        size = "Large"
                    },
                    new
                    {
                        type = "FactSet",
                        facts = new[]
                        {
                            new { title = "Severity", value = alert.Severity },
                            new { title = "Type", value = alert.Type },
                            new { title = "Timestamp", value = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") },
                            new { title = "Alert ID", value = alert.AlertId }
                        }
                    },
                    new
                    {
                        type = "TextBlock",
                        text = alert.Message,
                        wrap = true,
                        spacing = "Medium"
                    }
                },
                schema = "http://adaptivecards.io/schemas/adaptive-card.json"
            }
        };
    }

    private object CreateDigestCard(List<Alert> alerts, string period)
    {
        var criticalCount = alerts.Count(a => a.Severity == "Critical");
        var warningCount = alerts.Count(a => a.Severity == "Warning");
        var infoCount = alerts.Count(a => a.Severity == "Info");

        var alertItems = alerts.Select(a => new
        {
            type = "TextBlock",
            text = $"[{a.Severity}] {a.Type}: {a.Message}",
            wrap = true,
            color = a.Severity == "Critical" ? "Attention" : a.Severity == "Warning" ? "Warning" : "Default"
        }).ToArray();

        return new
        {
            contentType = "application/vnd.microsoft.card.adaptive",
            content = new
            {
                type = "AdaptiveCard",
                version = "1.4",
                body = new object[]
                {
                    new
                    {
                        type = "TextBlock",
                        text = $"AX Monitor {period} Digest",
                        weight = "Bolder",
                        size = "Large"
                    },
                    new
                    {
                        type = "FactSet",
                        facts = new[]
                        {
                            new { title = "Total Alerts", value = alerts.Count.ToString() },
                            new { title = "Critical", value = criticalCount.ToString() },
                            new { title = "Warning", value = warningCount.ToString() },
                            new { title = "Info", value = infoCount.ToString() }
                        }
                    },
                    new
                    {
                        type = "Container",
                        items = alertItems
                    }
                },
                schema = "http://adaptivecards.io/schemas/adaptive-card.json"
            }
        };
    }

    public async Task<bool> SendEscalationMessageAsync(string recipients, string escalationMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            // For Teams, we use the critical webhook URL for escalations
            // Recipients are handled via Teams channel mentions in the message
            var webhookUrl = GetWebhookUrl("Critical");
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("No Teams webhook URL configured for escalation");
                return false;
            }

            var card = new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                content = new
                {
                    type = "AdaptiveCard",
                    version = "1.4",
                    body = new object[]
                    {
                        new
                        {
                            type = "TextBlock",
                            text = "🚨 Alert Escalation",
                            weight = "Bolder",
                            size = "Large",
                            color = "Attention"
                        },
                        new
                        {
                            type = "TextBlock",
                            text = escalationMessage,
                            wrap = true,
                            spacing = "Medium"
                        }
                    },
                    schema = "http://adaptivecards.io/schemas/adaptive-card.json"
                }
            };

            var payload = new
            {
                type = "message",
                attachments = new[] { card }
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(webhookUrl, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams escalation message sent successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send Teams escalation message. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams escalation message");
            return false;
        }
    }
}

