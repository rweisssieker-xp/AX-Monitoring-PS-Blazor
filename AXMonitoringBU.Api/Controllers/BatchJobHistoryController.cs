using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;
using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/batch-jobs/history")]
public class BatchJobHistoryController : ControllerBase
{
    private readonly IAXDatabaseService _axDatabaseService;
    private readonly IBatchJobHistoryAnalysisService _analysisService;
    private readonly AXDbContext _context;
    private readonly ILogger<BatchJobHistoryController> _logger;

    public BatchJobHistoryController(
        IAXDatabaseService axDatabaseService,
        IBatchJobHistoryAnalysisService analysisService,
        AXDbContext context,
        ILogger<BatchJobHistoryController> logger)
    {
        _axDatabaseService = axDatabaseService;
        _analysisService = analysisService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBatchJobHistory(
        [FromQuery] string? captionPattern = null,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // Always use paging - page and pageSize have defaults
            var pagedResult = await _axDatabaseService.GetBatchJobHistoryPageAsync(page, pageSize, captionPattern, createdFrom);
            
            // Only use fallback if paged result is empty AND there was an error
            if (pagedResult.Items.Count == 0 && pagedResult.TotalCount == 0)
            {
                _logger.LogWarning("Paged result is empty, trying fallback method");
                // Fallback to non-paged result
                var history = await _axDatabaseService.GetBatchJobHistoryAsync(captionPattern, createdFrom);
                await EnrichWithAnalysisResultsAsync(history);

                return Ok(new
                {
                    history = history,
                    count = history.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            
            // Load analysis results for all items
            await EnrichWithAnalysisResultsAsync(pagedResult.Items);
            
            return Ok(new
            {
                history = pagedResult.Items,
                count = pagedResult.Items.Count,
                totalCount = pagedResult.TotalCount,
                page = pagedResult.Page,
                pageSize = pagedResult.PageSize,
                totalPages = pagedResult.TotalPages,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch job history: {ErrorMessage}", ex.Message);
            // Return empty list instead of error to allow page to load
            return Ok(new
            {
                history = new List<object>(),
                count = 0,
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }

    private async Task EnrichWithAnalysisResultsAsync(List<BatchJobHistory> history)
    {
        try
        {
            var analyses = await _context.BatchJobHistoryAnalyses
                .Where(a => history.Any(h => h.Caption == a.Caption && h.CreatedDateTime == a.CreatedDateTime))
                .ToListAsync();

            foreach (var item in history)
            {
                var analysis = analyses.FirstOrDefault(a => a.Caption == item.Caption && a.CreatedDateTime == item.CreatedDateTime);
                if (analysis != null)
                {
                    item.ErrorCategory = analysis.ErrorCategory;
                    item.ErrorSeverity = analysis.ErrorSeverity;
                    item.ErrorAnalysis = analysis.ErrorAnalysis;
                    item.ErrorSuggestions = analysis.ErrorSuggestions;
                    item.IsAnalyzed = true;
                    item.AnalyzedAt = analysis.AnalyzedAt;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enriching batch job history with analysis results");
        }
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeBatchJobError([FromBody] AnalyzeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ErrorReason) || string.IsNullOrEmpty(request.Caption))
            {
                return BadRequest(new { error = "Caption and ErrorReason are required" });
            }

            var analysis = await _analysisService.AnalyzeBatchJobErrorAsync(
                request.Caption, 
                request.CreatedDateTime, 
                request.ErrorReason, 
                cancellationToken);

            return Ok(new
            {
                analysis = analysis,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing batch job error: {ErrorMessage}", ex.Message);
            return StatusCode(500, new { error = "Failed to analyze error" });
        }
    }

    [HttpPost("analyze-batch")]
    public async Task<IActionResult> AnalyzeBatchJobErrorsBatch([FromBody] BatchAnalyzeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest(new { error = "Items array is required" });
            }

            var results = new List<object>();
            foreach (var item in request.Items.Take(10)) // Limit to 10 at a time
            {
                try
                {
                    var analysis = await _analysisService.AnalyzeBatchJobErrorAsync(
                        item.Caption, 
                        item.CreatedDateTime, 
                        item.ErrorReason, 
                        cancellationToken);
                    results.Add(new { 
                        caption = item.Caption,
                        createdDateTime = item.CreatedDateTime,
                        errorReason = item.ErrorReason, 
                        analysis 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing error reason: {ErrorReason}", item.ErrorReason);
                    results.Add(new { 
                        caption = item.Caption,
                        createdDateTime = item.CreatedDateTime,
                        errorReason = item.ErrorReason, 
                        error = ex.Message 
                    });
                }
            }

            return Ok(new
            {
                results = results,
                count = results.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch analysis");
            return StatusCode(500, new { error = "Failed to analyze errors" });
        }
    }

    [HttpGet("error-trends")]
    public async Task<IActionResult> GetErrorTrends(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var trends = await _analysisService.GetErrorTrendsAsync(fromDate, toDate);

            return Ok(new
            {
                trends = trends,
                count = trends.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error trends");
            return StatusCode(500, new { error = "Failed to retrieve error trends" });
        }
    }

    [HttpGet("error-summary")]
    public async Task<IActionResult> GetErrorSummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _analysisService.GetErrorStatisticsAsync(fromDate, toDate);
            var history = await _axDatabaseService.GetBatchJobHistoryAsync(null, fromDate ?? DateTime.UtcNow.AddDays(-30));
            var errors = history.Where(h => h.IsError).ToList();

            string summary = string.Empty;
            if (errors.Any())
            {
                summary = await _analysisService.GenerateErrorSummaryAsync(errors, cancellationToken);
            }

            return Ok(new
            {
                statistics = statistics,
                summary = summary,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error summary");
            return StatusCode(500, new { error = "Failed to retrieve error summary" });
        }
    }
}

public class AnalyzeRequest
{
    public string Caption { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
    public string ErrorReason { get; set; } = string.Empty;
}

public class BatchAnalyzeRequest
{
    public List<AnalyzeItem> Items { get; set; } = new();
}

public class AnalyzeItem
{
    public string Caption { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
    public string ErrorReason { get; set; } = string.Empty;
}
