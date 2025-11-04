using AXMonitoringBU.Api.Models;
using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;

namespace AXMonitoringBU.Api.Services;

public interface IBatchJobHistoryAnalysisService
{
    Task<ErrorAnalysisResult> AnalyzeBatchJobErrorAsync(string caption, DateTime? createdDateTime, string errorReason, CancellationToken cancellationToken = default);
    Task<ErrorAnalysisResult?> GetAnalysisAsync(string caption, DateTime? createdDateTime);
    Task SaveAnalysisAsync(string caption, DateTime? createdDateTime, string errorReason, ErrorAnalysisResult analysis);
    Task<List<ErrorTrend>> GetErrorTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<string> GenerateErrorSummaryAsync(List<BatchJobHistory> errors, CancellationToken cancellationToken = default);
}

public class BatchJobHistoryAnalysisService : IBatchJobHistoryAnalysisService
{
    private readonly IAXDatabaseService _axDatabaseService;
    private readonly IOpenAIService _openAIService;
    private readonly AXDbContext _context;
    private readonly ILogger<BatchJobHistoryAnalysisService> _logger;

    public BatchJobHistoryAnalysisService(
        IAXDatabaseService axDatabaseService,
        IOpenAIService openAIService,
        AXDbContext context,
        ILogger<BatchJobHistoryAnalysisService> logger)
    {
        _axDatabaseService = axDatabaseService;
        _openAIService = openAIService;
        _context = context;
        _logger = logger;
    }

    public async Task<ErrorAnalysisResult> AnalyzeBatchJobErrorAsync(string caption, DateTime? createdDateTime, string errorReason, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if analysis already exists
            var existing = await GetAnalysisAsync(caption, createdDateTime);
            if (existing != null)
            {
                _logger.LogInformation("Using cached analysis for {Caption}", caption);
                return existing;
            }

            // Perform new analysis
            var analysis = await _openAIService.AnalyzeErrorAsync(errorReason, cancellationToken);
            
            // Save analysis
            await SaveAnalysisAsync(caption, createdDateTime, errorReason, analysis);
            
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing batch job error");
            throw;
        }
    }

    public async Task<ErrorAnalysisResult?> GetAnalysisAsync(string caption, DateTime? createdDateTime)
    {
        try
        {
            var analysis = await _context.BatchJobHistoryAnalyses
                .FirstOrDefaultAsync(a => a.Caption == caption && a.CreatedDateTime == createdDateTime);
            
            if (analysis == null) return null;
            
            return new ErrorAnalysisResult
            {
                Category = analysis.ErrorCategory ?? "Unknown",
                Severity = analysis.ErrorSeverity ?? "Info",
                Explanation = analysis.ErrorAnalysis ?? "",
                Suggestions = analysis.ErrorSuggestions ?? "",
                AnalyzedAt = analysis.AnalyzedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analysis for {Caption}", caption);
            return null;
        }
    }

    public async Task SaveAnalysisAsync(string caption, DateTime? createdDateTime, string errorReason, ErrorAnalysisResult analysis)
    {
        try
        {
            var existing = await _context.BatchJobHistoryAnalyses
                .FirstOrDefaultAsync(a => a.Caption == caption && a.CreatedDateTime == createdDateTime);
            
            if (existing != null)
            {
                // Update existing
                existing.ErrorReason = errorReason;
                existing.ErrorCategory = analysis.Category;
                existing.ErrorSeverity = analysis.Severity;
                existing.ErrorAnalysis = analysis.Explanation;
                existing.ErrorSuggestions = analysis.Suggestions;
                existing.AnalyzedAt = analysis.AnalyzedAt;
                _context.BatchJobHistoryAnalyses.Update(existing);
            }
            else
            {
                // Create new
                var newAnalysis = new BatchJobHistoryAnalysis
                {
                    Caption = caption,
                    CreatedDateTime = createdDateTime,
                    ErrorReason = errorReason,
                    ErrorCategory = analysis.Category,
                    ErrorSeverity = analysis.Severity,
                    ErrorAnalysis = analysis.Explanation,
                    ErrorSuggestions = analysis.Suggestions,
                    AnalyzedAt = analysis.AnalyzedAt
                };
                _context.BatchJobHistoryAnalyses.Add(newAnalysis);
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved analysis for {Caption}", caption);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving analysis for {Caption}", caption);
            throw;
        }
    }

    public async Task<List<ErrorTrend>> GetErrorTrendsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var history = await _axDatabaseService.GetBatchJobHistoryAsync(null, from);
            
            // Enrich with analysis results from monitoring database
            var analyses = await _context.BatchJobHistoryAnalyses
                .Where(a => a.CreatedDateTime >= from && a.CreatedDateTime <= to)
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
            
            var errors = history
                .Where(h => h.IsError && h.CreatedDateTime >= from && h.CreatedDateTime <= to)
                .ToList();

            var trends = errors
                .GroupBy(e => e.CreatedDateTime?.Date ?? DateTime.UtcNow.Date)
                .Select(g => new ErrorTrend
                {
                    Date = g.Key,
                    ErrorCount = g.Count(),
                    Categories = g
                        .Where(e => !string.IsNullOrEmpty(e.ErrorCategory))
                        .GroupBy(e => e.ErrorCategory ?? "Unknown")
                        .ToDictionary(x => x.Key, x => x.Count())
                })
                .OrderBy(t => t.Date)
                .ToList();

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error trends");
            return new List<ErrorTrend>();
        }
    }

    public async Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var history = await _axDatabaseService.GetBatchJobHistoryAsync(null, from);
            
            // Enrich with analysis results from monitoring database
            var analyses = await _context.BatchJobHistoryAnalyses
                .Where(a => a.CreatedDateTime >= from && a.CreatedDateTime <= to)
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
            
            var allJobs = history.Where(h => h.CreatedDateTime >= from && h.CreatedDateTime <= to).ToList();
            var errors = allJobs.Where(h => h.IsError).ToList();

            var totalJobs = allJobs.Count;
            var totalErrors = errors.Count;
            var successRate = totalJobs > 0 ? (100.0 - (totalErrors * 100.0 / totalJobs)) : 100.0;

            var categoryCounts = errors
                .Where(e => !string.IsNullOrEmpty(e.ErrorCategory))
                .GroupBy(e => e.ErrorCategory ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var topCategories = categoryCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => new CategoryCount { Category = kvp.Key, Count = kvp.Value })
                .ToList();

            var severityCounts = errors
                .Where(e => !string.IsNullOrEmpty(e.ErrorSeverity))
                .GroupBy(e => e.ErrorSeverity ?? "Info")
                .ToDictionary(g => g.Key, g => g.Count());

            return new ErrorStatistics
            {
                TotalJobs = totalJobs,
                TotalErrors = totalErrors,
                SuccessRate = Math.Round(successRate, 2),
                ErrorRate = Math.Round(100.0 - successRate, 2),
                TopCategories = topCategories,
                SeverityCounts = severityCounts,
                PeriodFrom = from,
                PeriodTo = to
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error statistics");
            // Return empty statistics instead of throwing
            return new ErrorStatistics
            {
                TotalJobs = 0,
                TotalErrors = 0,
                SuccessRate = 0.0,
                ErrorRate = 0.0,
                TopCategories = new List<CategoryCount>(),
                SeverityCounts = new Dictionary<string, int>(),
                PeriodFrom = fromDate ?? DateTime.UtcNow.AddDays(-30),
                PeriodTo = toDate ?? DateTime.UtcNow
            };
        }
    }

    public async Task<string> GenerateErrorSummaryAsync(List<BatchJobHistory> errors, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!errors.Any())
            {
                return "Keine Fehler im ausgewählten Zeitraum.";
            }

            var errorReasons = errors
                .Where(e => !string.IsNullOrEmpty(e.Reason))
                .Select(e => e.Reason!)
                .Take(10) // Limit to avoid token limits
                .ToList();

            var prompt = $@"Erstelle eine Zusammenfassung der folgenden Batch Job Fehler in German. 
Fasse die häufigsten Fehlerarten zusammen und gib eine kurze Übersicht (3-4 Sätze).

Fehler:
{string.Join("\n", errorReasons.Select((r, i) => $"{i + 1}. {r}"))}

Zusammenfassung:";

            var summary = await _openAIService.ExplainErrorAsync(prompt, cancellationToken);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating error summary");
            return "Fehler beim Generieren der Zusammenfassung.";
        }
    }
}

public class ErrorTrend
{
    public DateTime Date { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int> Categories { get; set; } = new();
}

public class ErrorStatistics
{
    public int TotalJobs { get; set; }
    public int TotalErrors { get; set; }
    public double SuccessRate { get; set; }
    public double ErrorRate { get; set; }
    public List<CategoryCount> TopCategories { get; set; } = new();
    public Dictionary<string, int> SeverityCounts { get; set; } = new();
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
}

public class CategoryCount
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

