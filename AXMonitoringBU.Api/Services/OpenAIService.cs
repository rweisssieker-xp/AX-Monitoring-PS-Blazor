using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AXMonitoringBU.Api.Services;

public interface IOpenAIService
{
    Task<ErrorAnalysisResult> AnalyzeErrorAsync(string errorReason, CancellationToken cancellationToken = default);
    Task<List<ErrorAnalysisResult>> AnalyzeErrorsBatchAsync(List<string> errorReasons, CancellationToken cancellationToken = default);
    Task<string> ClassifyErrorAsync(string errorReason, CancellationToken cancellationToken = default);
    Task<string> ExplainErrorAsync(string errorReason, CancellationToken cancellationToken = default);
    Task<string> GetErrorSuggestionsAsync(string errorReason, CancellationToken cancellationToken = default);
}

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly int _maxTokens;

    public OpenAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key not configured");
        _model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
        _maxTokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "500");

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<ErrorAnalysisResult> AnalyzeErrorAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await ClassifyErrorAsync(errorReason, cancellationToken);
            var explanation = await ExplainErrorAsync(errorReason, cancellationToken);
            var suggestions = await GetErrorSuggestionsAsync(errorReason, cancellationToken);

            var severity = DetermineSeverity(category, errorReason);

            return new ErrorAnalysisResult
            {
                Category = category,
                Severity = severity,
                Explanation = explanation,
                Suggestions = suggestions,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing error reason: {ErrorReason}", errorReason);
            return new ErrorAnalysisResult
            {
                Category = "Unknown",
                Severity = "Info",
                Explanation = "Fehleranalyse konnte nicht durchgeführt werden.",
                Suggestions = "Bitte Fehler manuell überprüfen.",
                AnalyzedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<List<ErrorAnalysisResult>> AnalyzeErrorsBatchAsync(List<string> errorReasons, CancellationToken cancellationToken = default)
    {
        var results = new List<ErrorAnalysisResult>();
        
        foreach (var errorReason in errorReasons)
        {
            try
            {
                var result = await AnalyzeErrorAsync(errorReason, cancellationToken);
                results.Add(result);
                
                // Rate limiting: 1 request per second
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch analysis for: {ErrorReason}", errorReason);
                results.Add(new ErrorAnalysisResult
                {
                    Category = "Error",
                    Severity = "Info",
                    Explanation = "Analyse fehlgeschlagen",
                    Suggestions = "",
                    AnalyzedAt = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    public async Task<string> ClassifyErrorAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        var prompt = $@"Classify this batch job error reason into ONE of these categories:
- Database Lock
- Timeout
- Permission Error
- Data Validation
- Network Error
- Application Error
- Configuration Error
- Other

Error reason: {errorReason}

Respond with ONLY the category name, nothing else.";

        var response = await CallOpenAIAsync(prompt, cancellationToken);
        return response?.Trim() ?? "Other";
    }

    public async Task<string> ExplainErrorAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        var prompt = $@"Explain why this batch job error occurred in German. Be concise and technical.

Error reason: {errorReason}

Provide a 2-3 sentence explanation in German.";

        var response = await CallOpenAIAsync(prompt, cancellationToken);
        return response ?? "Keine Erklärung verfügbar.";
    }

    public async Task<string> GetErrorSuggestionsAsync(string errorReason, CancellationToken cancellationToken = default)
    {
        var prompt = $@"Provide 3-5 troubleshooting steps for this batch job error in German. Format as a numbered list.

Error reason: {errorReason}

Provide troubleshooting steps in German.";

        var response = await CallOpenAIAsync(prompt, cancellationToken);
        return response ?? "Keine Lösungsvorschläge verfügbar.";
    }

    private async Task<string?> CallOpenAIAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = _maxTokens,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken: cancellationToken);
            
            return responseBody?.choices?.FirstOrDefault()?.message?.content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw;
        }
    }

    private string DetermineSeverity(string category, string errorReason)
    {
        var lowerCategory = category.ToLower();
        var lowerReason = errorReason.ToLower();

        if (lowerCategory.Contains("database lock") || lowerCategory.Contains("timeout") || 
            lowerReason.Contains("critical") || lowerReason.Contains("fatal"))
        {
            return "Critical";
        }

        if (lowerCategory.Contains("permission") || lowerCategory.Contains("validation") ||
            lowerReason.Contains("error") || lowerReason.Contains("failed"))
        {
            return "Warning";
        }

        return "Info";
    }

    private class OpenAIResponse
    {
        public List<Choice>? choices { get; set; }
    }

    private class Choice
    {
        public Message? message { get; set; }
    }

    private class Message
    {
        public string? content { get; set; }
    }
}

public class ErrorAnalysisResult
{
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Explanation { get; set; } = string.Empty;
    public string Suggestions { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
}

