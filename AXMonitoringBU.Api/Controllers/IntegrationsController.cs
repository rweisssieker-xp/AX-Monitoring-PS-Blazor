using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class IntegrationsController : ControllerBase
{
    private readonly ITicketingService _ticketingService;
    private readonly IAlertService _alertService;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        ITicketingService ticketingService,
        IAlertService alertService,
        ILogger<IntegrationsController> logger)
    {
        _ticketingService = ticketingService;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Create a ticket from an alert
    /// </summary>
    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketingService.CreateTicketAsync(request, cancellationToken);
            return Ok(new
            {
                ticket = ticket,
                message = "Ticket created successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            return StatusCode(500, new { error = "Failed to create ticket", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a ticket from an alert
    /// </summary>
    [HttpPost("alerts/{alertId}/ticket")]
    public async Task<IActionResult> CreateTicketFromAlert(int alertId, [FromBody] CreateTicketFromAlertRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var alert = await _alertService.GetAlertByIdAsync(alertId);
            if (alert == null)
            {
                return NotFound(new { message = $"Alert {alertId} not found" });
            }

            var ticketRequest = new CreateTicketRequest
            {
                Title = $"[{alert.Severity}] {alert.Type}",
                Description = alert.Message,
                Priority = alert.Severity.ToLower() switch
                {
                    "critical" => "critical",
                    "warning" => "high",
                    _ => "medium"
                },
                System = request.System,
                Category = request.Category ?? "Monitoring",
                AssignmentGroup = request.AssignmentGroup
            };

            var ticket = await _ticketingService.CreateTicketAsync(ticketRequest, cancellationToken);

            // Optionally update alert with ticket reference
            // await _alertService.UpdateAlertMetadataAsync(alertId, new { ticket_id = ticket.TicketId });

            return Ok(new
            {
                ticket = ticket,
                alert_id = alertId,
                message = "Ticket created from alert",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket from alert {AlertId}", alertId);
            return StatusCode(500, new { error = "Failed to create ticket from alert", message = ex.Message });
        }
    }

    /// <summary>
    /// Get ticket by ID
    /// </summary>
    [HttpGet("tickets/{ticketId}")]
    public async Task<IActionResult> GetTicket(string ticketId, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _ticketingService.GetTicketAsync(ticketId, cancellationToken);
            if (ticket == null)
            {
                return NotFound(new { message = $"Ticket {ticketId} not found" });
            }
            return Ok(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket {TicketId}", ticketId);
            return StatusCode(500, new { error = "Failed to retrieve ticket" });
        }
    }

    /// <summary>
    /// Get all tickets
    /// </summary>
    [HttpGet("tickets")]
    public async Task<IActionResult> GetTickets([FromQuery] string? status, CancellationToken cancellationToken)
    {
        try
        {
            var tickets = await _ticketingService.GetTicketsAsync(status, cancellationToken);
            return Ok(new
            {
                tickets = tickets,
                count = tickets.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets");
            return StatusCode(500, new { error = "Failed to retrieve tickets" });
        }
    }

    /// <summary>
    /// Update ticket status
    /// </summary>
    [HttpPut("tickets/{ticketId}")]
    public async Task<IActionResult> UpdateTicket(string ticketId, [FromBody] UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _ticketingService.UpdateTicketAsync(ticketId, request, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = $"Ticket {ticketId} not found" });
            }
            return Ok(new { message = "Ticket updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket {TicketId}", ticketId);
            return StatusCode(500, new { error = "Failed to update ticket" });
        }
    }
}

public class CreateTicketFromAlertRequest
{
    public string? System { get; set; }
    public string? Category { get; set; }
    public string? AssignmentGroup { get; set; }
}

