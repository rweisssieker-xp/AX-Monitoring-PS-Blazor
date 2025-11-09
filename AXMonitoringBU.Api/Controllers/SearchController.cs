using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly AXDbContext _context;
    private readonly ILogger<SearchController> _logger;

    public SearchController(AXDbContext context, ILogger<SearchController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        try
        {
            var results = new List<SearchResult>();

            // Search BatchJobs
            var batchJobs = await _context.BatchJobs
                .Where(b => b.Name.Contains(query) || b.BatchJobId.Contains(query) || b.Status.Contains(query))
                .Take(limit)
                .Select(b => new SearchResult
                {
                    EntityType = "BatchJob",
                    EntityId = b.BatchJobId,
                    Title = b.Name,
                    Description = $"Status: {b.Status}, AOS: {b.AosServer}",
                    Url = $"/batch-jobs/{b.Id}",
                    Timestamp = b.CreatedAt
                })
                .ToListAsync();

            results.AddRange(batchJobs);

            // Search Sessions
            var sessions = await _context.Sessions
                .Where(s => s.SessionId.Contains(query) || s.UserId.Contains(query) || s.Status.Contains(query))
                .Take(limit)
                .Select(s => new SearchResult
                {
                    EntityType = "Session",
                    EntityId = s.SessionId,
                    Title = $"Session {s.SessionId}",
                    Description = $"User: {s.UserId}, Status: {s.Status}, AOS: {s.AosServer}",
                    Url = $"/sessions/{s.Id}",
                    Timestamp = s.CreatedAt
                })
                .ToListAsync();

            results.AddRange(sessions);

            // Search Alerts
            var alerts = await _context.Alerts
                .Where(a => a.Message.Contains(query) || a.Type.Contains(query) || a.AlertId.Contains(query))
                .Take(limit)
                .Select(a => new SearchResult
                {
                    EntityType = "Alert",
                    EntityId = a.AlertId,
                    Title = $"{a.Type} - {a.Severity}",
                    Description = a.Message,
                    Url = $"/alerts/{a.Id}",
                    Timestamp = a.CreatedAt
                })
                .ToListAsync();

            results.AddRange(alerts);

            // Order by relevance (exact matches first, then by timestamp)
            var orderedResults = results
                .OrderByDescending(r => r.Title.Equals(query, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(r => r.Timestamp)
                .Take(limit)
                .ToList();

            return Ok(new { Query = query, Results = orderedResults, Count = orderedResults.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search for query: {Query}", query);
            return StatusCode(500, "An error occurred while performing the search");
        }
    }
}

public class SearchResult
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}


