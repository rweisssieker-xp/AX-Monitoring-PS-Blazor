namespace AXMonitoringBU.Api.Services;

public class DummyOpenAIService : IOpenAIService
{
    public Task<ErrorAnalysisResult> AnalyzeErrorAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ErrorAnalysisResult
        {
            Category = "Unknown",
            Severity = "Info",
            Explanation = "OpenAI analysis is disabled.",
            Suggestions = "Enable OpenAI:AnalysisEnabled in configuration.",
            AnalyzedAt = DateTime.UtcNow
        });
    }

    public Task<List<ErrorAnalysisResult>> AnalyzeErrorsBatchAsync(List<string> errorReasons, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<ErrorAnalysisResult>());
    }

    public Task<string> ClassifyErrorAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Unknown");
    }

    public Task<string> ExplainErrorAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("OpenAI analysis is disabled.");
    }

    public Task<string> GetErrorSuggestionsAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Enable OpenAI analysis in configuration.");
    }
}

