using System.Collections.Concurrent;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IBackgroundAnalysisService
{
    Task<string> QueueAnalysisAsync(string errorReason);
    Task<AnalysisStatus?> GetAnalysisStatusAsync(string analysisId);
    Task<List<AnalysisStatus>> GetPendingAnalysesAsync();
}

public class BackgroundAnalysisService : IBackgroundAnalysisService
{
    private readonly ConcurrentDictionary<string, AnalysisStatus> _analysisQueue;
    private readonly IOpenAIService _openAIService;
    private readonly ILogger<BackgroundAnalysisService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public BackgroundAnalysisService(
        IOpenAIService openAIService,
        ILogger<BackgroundAnalysisService> logger)
    {
        _analysisQueue = new ConcurrentDictionary<string, AnalysisStatus>();
        _openAIService = openAIService;
        _logger = logger;
        _semaphore = new SemaphoreSlim(3, 3); // Max 3 concurrent analyses

        // Start background processing
        _ = Task.Run(ProcessAnalysisQueueAsync);
    }

    public Task<string> QueueAnalysisAsync(string errorReason)
    {
        var analysisId = Guid.NewGuid().ToString();
        var status = new AnalysisStatus
        {
            AnalysisId = analysisId,
            ErrorReason = errorReason,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _analysisQueue.TryAdd(analysisId, status);
        _logger.LogInformation("Queued analysis {AnalysisId} for error reason", analysisId);

        return Task.FromResult(analysisId);
    }

    public Task<AnalysisStatus?> GetAnalysisStatusAsync(string analysisId)
    {
        _analysisQueue.TryGetValue(analysisId, out var status);
        return Task.FromResult(status);
    }

    public Task<List<AnalysisStatus>> GetPendingAnalysesAsync()
    {
        var pending = _analysisQueue.Values
            .Where(s => s.Status == "Pending" || s.Status == "Analyzing")
            .OrderBy(s => s.CreatedAt)
            .ToList();

        return Task.FromResult(pending);
    }

    private async Task ProcessAnalysisQueueAsync()
    {
        while (true)
        {
            try
            {
                var pending = _analysisQueue.Values
                    .Where(s => s.Status == "Pending")
                    .OrderBy(s => s.CreatedAt)
                    .Take(10)
                    .ToList();

                var tasks = pending.Select(async status =>
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        await ProcessAnalysisAsync(status);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                await Task.Delay(5000); // Check every 5 seconds
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing analysis queue");
                await Task.Delay(10000); // Wait longer on error
            }
        }
    }

    private async Task ProcessAnalysisAsync(AnalysisStatus status)
    {
        try
        {
            status.Status = "Analyzing";
            status.StartedAt = DateTime.UtcNow;

            var analysis = await _openAIService.AnalyzeErrorAsync(status.ErrorReason);
            
            status.Status = "Completed";
            status.CompletedAt = DateTime.UtcNow;
            status.ErrorCategory = analysis.Category;
            status.ErrorSeverity = analysis.Severity;
            status.ErrorAnalysis = analysis.Explanation;
            status.ErrorSuggestions = analysis.Suggestions;

            _logger.LogInformation("Completed analysis {AnalysisId}", status.AnalysisId);
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
            status.CompletedAt = DateTime.UtcNow;
            
            _logger.LogError(ex, "Failed to analyze {AnalysisId}", status.AnalysisId);

            // Retry logic: retry up to 3 times
            if (status.RetryCount < 3)
            {
                status.RetryCount++;
                status.Status = "Pending";
                status.ErrorMessage = null;
                await Task.Delay(5000 * status.RetryCount); // Exponential backoff
            }
        }
    }
}

public class AnalysisStatus
{
    public string AnalysisId { get; set; } = string.Empty;
    public string ErrorReason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Analyzing, Completed, Failed
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorCategory { get; set; }
    public string? ErrorSeverity { get; set; }
    public string? ErrorAnalysis { get; set; }
    public string? ErrorSuggestions { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

