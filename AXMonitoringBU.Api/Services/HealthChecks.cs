using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Net.Http;
using System.Text.Json;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AXMonitoringBU.Api.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AXDbContext>();
            
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database connection failed");
            }

            // Simple query to verify database is responsive
            await dbContext.BatchJobs.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}

public class EmailHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailHealthCheck> _logger;

    public EmailHealthCheck(IConfiguration configuration, ILogger<EmailHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var enabled = _configuration["Alerts:Email:Enabled"] == "true";
            if (!enabled)
            {
                return HealthCheckResult.Healthy("Email alerts are disabled");
            }

            var smtpServer = _configuration["Alerts:Email:SmtpServer"];
            var smtpPort = int.Parse(_configuration["Alerts:Email:SmtpPort"] ?? "587");

            if (string.IsNullOrEmpty(smtpServer))
            {
                return HealthCheckResult.Degraded("SMTP server not configured");
            }

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.Auto, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return HealthCheckResult.Healthy("Email service is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email health check failed");
            return HealthCheckResult.Degraded("Email service check failed", ex);
        }
    }
}

public class TeamsHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TeamsHealthCheck> _logger;

    public TeamsHealthCheck(
        IConfiguration configuration, 
        IHttpClientFactory httpClientFactory,
        ILogger<TeamsHealthCheck> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var enabled = _configuration["Alerts:Teams:Enabled"] == "true";
            if (!enabled)
            {
                return HealthCheckResult.Healthy("Teams notifications are disabled");
            }

            var webhookUrl = _configuration["Alerts:Teams:CriticalWebhookUrl"] 
                ?? _configuration["Alerts:Teams:WarningWebhookUrl"] 
                ?? _configuration["Alerts:Teams:InfoWebhookUrl"];

            if (string.IsNullOrEmpty(webhookUrl))
            {
                return HealthCheckResult.Degraded("Teams webhook URL not configured");
            }

            // Teams webhooks don't have a ping endpoint, so we just verify the URL is valid
            var isValidUrl = Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) 
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            if (!isValidUrl)
            {
                return HealthCheckResult.Degraded("Invalid Teams webhook URL format");
            }

            return HealthCheckResult.Healthy("Teams webhook URL is configured");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Teams health check failed");
            return HealthCheckResult.Degraded("Teams service check failed", ex);
        }
    }
}

public class TicketingHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TicketingHealthCheck> _logger;

    public TicketingHealthCheck(
        IConfiguration configuration, 
        IHttpClientFactory httpClientFactory,
        ILogger<TicketingHealthCheck> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var defaultSystem = _configuration["Integrations:Ticketing:DefaultSystem"]?.ToLower() ?? "";

            if (string.IsNullOrEmpty(defaultSystem))
            {
                return HealthCheckResult.Healthy("Ticketing system not configured");
            }

            var data = new Dictionary<string, object>();

            switch (defaultSystem)
            {
                case "servicenow":
                    var servicenowUrl = _configuration["Integrations:Ticketing:ServiceNow:BaseUrl"];
                    if (string.IsNullOrEmpty(servicenowUrl))
                    {
                        return HealthCheckResult.Degraded("ServiceNow URL not configured");
                    }
                    data["service"] = "ServiceNow";
                    data["url"] = servicenowUrl;
                    break;

                case "jira":
                    var jiraUrl = _configuration["Integrations:Ticketing:Jira:BaseUrl"];
                    if (string.IsNullOrEmpty(jiraUrl))
                    {
                        return HealthCheckResult.Degraded("Jira URL not configured");
                    }
                    data["service"] = "Jira";
                    data["url"] = jiraUrl;
                    break;

                case "azuredevops":
                    var adoUrl = _configuration["Integrations:Ticketing:AzureDevOps:BaseUrl"];
                    if (string.IsNullOrEmpty(adoUrl))
                    {
                        return HealthCheckResult.Degraded("Azure DevOps URL not configured");
                    }
                    data["service"] = "Azure DevOps";
                    data["url"] = adoUrl;
                    break;

                default:
                    return HealthCheckResult.Degraded($"Unknown ticketing system: {defaultSystem}");
            }

            return HealthCheckResult.Healthy($"Ticketing system ({data["service"]}) is configured", data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ticketing health check failed");
            return HealthCheckResult.Degraded("Ticketing service check failed", ex);
        }
    }
}

