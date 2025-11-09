using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IEmailAlertService _emailService;
    private readonly ITeamsNotificationService _teamsService;
    private readonly IAlertService _alertService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IEmailAlertService emailService,
        ITeamsNotificationService teamsService,
        IAlertService alertService,
        ILogger<NotificationsController> logger)
    {
        _emailService = emailService;
        _teamsService = teamsService;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Send notification for an alert
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            Alert? alert = null;
            
            if (request.AlertId.HasValue)
            {
                alert = await _alertService.GetAlertByIdAsync(request.AlertId.Value);
                if (alert == null)
                {
                    return NotFound(new { message = $"Alert with ID {request.AlertId} not found" });
                }
            }
            else if (!string.IsNullOrEmpty(request.AlertData))
            {
                // Parse alert data from JSON string
                var alertData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(request.AlertData);
                if (alertData != null)
                {
                    alert = new Alert
                    {
                        AlertId = alertData.ContainsKey("alertId") ? alertData["alertId"].ToString()! : $"ALERT_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                        Type = alertData.ContainsKey("type") ? alertData["type"].ToString()! : "Unknown",
                        Severity = alertData.ContainsKey("severity") ? alertData["severity"].ToString()! : "Info",
                        Message = alertData.ContainsKey("message") ? alertData["message"].ToString()! : "",
                        Status = "Active",
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            else
            {
                return BadRequest(new { message = "Either AlertId or AlertData must be provided" });
            }

            if (alert == null)
            {
                return BadRequest(new { message = "Unable to create alert from provided data" });
            }

            var results = new Dictionary<string, bool>();

            if (request.Type == "email" || request.Type == "all")
            {
                results["email"] = await _emailService.SendAlertAsync(alert, cancellationToken);
            }

            if (request.Type == "teams" || request.Type == "all")
            {
                results["teams"] = await _teamsService.SendAlertAsync(alert, cancellationToken);
            }

            return Ok(new
            {
                message = $"Notification sent via {request.Type}",
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, new { message = "Failed to send notification", error = ex.Message });
        }
    }

    /// <summary>
    /// Send digest notification
    /// </summary>
    [HttpPost("digest")]
    public async Task<IActionResult> SendDigest([FromBody] SendDigestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Alert> alerts;
            
            if (request.Status != null)
            {
                alerts = await _alertService.GetAlertsAsync(request.Status);
            }
            else
            {
                alerts = await _alertService.GetAlertsAsync();
            }

            var alertsList = alerts.ToList();
            if (!alertsList.Any())
            {
                return Ok(new { message = "No alerts to send", count = 0 });
            }

            var results = new Dictionary<string, bool>();

            if (request.Type == "email" || request.Type == "all")
            {
                results["email"] = await _emailService.SendDigestAsync(alertsList, request.Period ?? "hourly", cancellationToken);
            }

            if (request.Type == "teams" || request.Type == "all")
            {
                results["teams"] = await _teamsService.SendDigestAsync(alertsList, request.Period ?? "hourly", cancellationToken);
            }

            return Ok(new
            {
                message = $"Digest sent via {request.Type}",
                alertsCount = alertsList.Count,
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending digest");
            return StatusCode(500, new { message = "Failed to send digest", error = ex.Message });
        }
    }
}

public class SendNotificationRequest
{
    public string Type { get; set; } = "all"; // email, teams, all
    public int? AlertId { get; set; }
    public string? AlertData { get; set; } // JSON string with alert data
}

public class SendDigestRequest
{
    public string Type { get; set; } = "all"; // email, teams, all
    public string? Period { get; set; } = "hourly"; // hourly, daily, weekly
    public string? Status { get; set; } // Active, Resolved, etc.
}

