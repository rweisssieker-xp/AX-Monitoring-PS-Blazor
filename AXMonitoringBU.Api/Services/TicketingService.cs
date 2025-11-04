using System.Net.Http.Json;
using System.Text;

namespace AXMonitoringBU.Api.Services;

public interface ITicketingService
{
    Task<TicketResult> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);
    Task<TicketResult?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default);
    Task<bool> UpdateTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken = default);
    Task<List<TicketResult>> GetTicketsAsync(string? status = null, CancellationToken cancellationToken = default);
}

public class TicketingService : ITicketingService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TicketingService> _logger;

    public TicketingService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<TicketingService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TicketResult> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var system = request.System ?? GetDefaultSystem();
            
            return system switch
            {
                "servicenow" => await CreateServiceNowTicketAsync(request, cancellationToken),
                "jira" => await CreateJiraTicketAsync(request, cancellationToken),
                "azure_devops" => await CreateAzureDevOpsTicketAsync(request, cancellationToken),
                _ => throw new ArgumentException($"Unsupported ticketing system: {system}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            throw;
        }
    }

    public async Task<TicketResult?> GetTicketAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Determine system from ticket ID format or configuration
            var system = GetDefaultSystem();
            
            return system switch
            {
                "servicenow" => await GetServiceNowTicketAsync(ticketId, cancellationToken),
                "jira" => await GetJiraTicketAsync(ticketId, cancellationToken),
                "azure_devops" => await GetAzureDevOpsTicketAsync(ticketId, cancellationToken),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket {TicketId}", ticketId);
            return null;
        }
    }

    public async Task<bool> UpdateTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var system = GetDefaultSystem();
            
            return system switch
            {
                "servicenow" => await UpdateServiceNowTicketAsync(ticketId, request, cancellationToken),
                "jira" => await UpdateJiraTicketAsync(ticketId, request, cancellationToken),
                "azure_devops" => await UpdateAzureDevOpsTicketAsync(ticketId, request, cancellationToken),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket {TicketId}", ticketId);
            return false;
        }
    }

    public async Task<List<TicketResult>> GetTicketsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var system = GetDefaultSystem();
            
            return system switch
            {
                "servicenow" => await GetServiceNowTicketsAsync(status, cancellationToken),
                "jira" => await GetJiraTicketsAsync(status, cancellationToken),
                "azure_devops" => await GetAzureDevOpsTicketsAsync(status, cancellationToken),
                _ => new List<TicketResult>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets");
            return new List<TicketResult>();
        }
    }

    private string GetDefaultSystem() => _configuration["Integrations:Ticketing:DefaultSystem"] ?? "servicenow";

    private async Task<TicketResult> CreateServiceNowTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Integrations:Ticketing:ServiceNow:BaseUrl"];
        var username = _configuration["Integrations:Ticketing:ServiceNow:Username"];
        var password = _configuration["Integrations:Ticketing:ServiceNow:Password"];
        var table = _configuration["Integrations:Ticketing:ServiceNow:Table"] ?? "incident";

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("ServiceNow configuration is missing");
        }

        var client = _httpClientFactory.CreateClient();
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            short_description = request.Title,
            description = request.Description,
            urgency = MapPriorityToServiceNowUrgency(request.Priority),
            impact = MapPriorityToServiceNowImpact(request.Priority),
            category = request.Category ?? "Infrastructure",
            assignment_group = request.AssignmentGroup
        };

        var response = await client.PostAsJsonAsync($"{baseUrl}/api/now/table/{table}", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceNowResponse>(cancellationToken: cancellationToken);
        
        return new TicketResult
        {
            TicketId = result?.result?.sys_id ?? "",
            ExternalId = result?.result?.number ?? "",
            System = "servicenow",
            Status = result?.result?.state ?? "New",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<TicketResult> CreateJiraTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Integrations:Ticketing:Jira:BaseUrl"];
        var username = _configuration["Integrations:Ticketing:Jira:Username"];
        var apiToken = _configuration["Integrations:Ticketing:Jira:ApiToken"];
        var projectKey = _configuration["Integrations:Ticketing:Jira:ProjectKey"] ?? "MON";

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(apiToken))
        {
            throw new InvalidOperationException("Jira configuration is missing");
        }

        var client = _httpClientFactory.CreateClient();
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiToken}"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            fields = new
            {
                project = new { key = projectKey },
                summary = request.Title,
                description = request.Description,
                issuetype = new { name = "Incident" },
                priority = new { name = MapPriorityToJiraPriority(request.Priority) }
            }
        };

        var response = await client.PostAsJsonAsync($"{baseUrl}/rest/api/3/issue", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JiraResponse>(cancellationToken: cancellationToken);
        
        return new TicketResult
        {
            TicketId = result?.id ?? "",
            ExternalId = result?.key ?? "",
            System = "jira",
            Status = "To Do",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<TicketResult> CreateAzureDevOpsTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["Integrations:Ticketing:AzureDevOps:BaseUrl"];
        var pat = _configuration["Integrations:Ticketing:AzureDevOps:PersonalAccessToken"];
        var organization = _configuration["Integrations:Ticketing:AzureDevOps:Organization"];
        var project = _configuration["Integrations:Ticketing:AzureDevOps:Project"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
        {
            throw new InvalidOperationException("Azure DevOps configuration is missing");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", 
            Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}")));
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new[]
        {
            new
            {
                op = "add",
                path = "/fields/System.Title",
                value = request.Title
            },
            new
            {
                op = "add",
                path = "/fields/System.Description",
                value = request.Description
            }
        };

        var response = await client.PostAsJsonAsync(
            $"{baseUrl}/{organization}/{project}/_apis/wit/workitems/$Incident?api-version=6.0", 
            payload, 
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureDevOpsResponse>(cancellationToken: cancellationToken);
        
        return new TicketResult
        {
            TicketId = result?.id?.ToString() ?? "",
            ExternalId = result?.id?.ToString() ?? "",
            System = "azure_devops",
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<TicketResult?> GetServiceNowTicketAsync(string ticketId, CancellationToken cancellationToken)
    {
        // TODO: Implement ServiceNow ticket retrieval
        await Task.CompletedTask;
        return null;
    }

    private async Task<TicketResult?> GetJiraTicketAsync(string ticketId, CancellationToken cancellationToken)
    {
        // TODO: Implement Jira ticket retrieval
        await Task.CompletedTask;
        return null;
    }

    private async Task<TicketResult?> GetAzureDevOpsTicketAsync(string ticketId, CancellationToken cancellationToken)
    {
        // TODO: Implement Azure DevOps ticket retrieval
        await Task.CompletedTask;
        return null;
    }

    private async Task<bool> UpdateServiceNowTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        // TODO: Implement ServiceNow ticket update
        await Task.CompletedTask;
        return false;
    }

    private async Task<bool> UpdateJiraTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        // TODO: Implement Jira ticket update
        await Task.CompletedTask;
        return false;
    }

    private async Task<bool> UpdateAzureDevOpsTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        // TODO: Implement Azure DevOps ticket update
        await Task.CompletedTask;
        return false;
    }

    private async Task<List<TicketResult>> GetServiceNowTicketsAsync(string? status, CancellationToken cancellationToken)
    {
        // TODO: Implement ServiceNow tickets list
        await Task.CompletedTask;
        return new List<TicketResult>();
    }

    private async Task<List<TicketResult>> GetJiraTicketsAsync(string? status, CancellationToken cancellationToken)
    {
        // TODO: Implement Jira tickets list
        await Task.CompletedTask;
        return new List<TicketResult>();
    }

    private async Task<List<TicketResult>> GetAzureDevOpsTicketsAsync(string? status, CancellationToken cancellationToken)
    {
        // TODO: Implement Azure DevOps tickets list
        await Task.CompletedTask;
        return new List<TicketResult>();
    }

    private string MapPriorityToServiceNowUrgency(string priority) => priority.ToLower() switch
    {
        "critical" => "1",
        "high" => "2",
        "medium" => "3",
        _ => "4"
    };

    private string MapPriorityToServiceNowImpact(string priority) => priority.ToLower() switch
    {
        "critical" => "1",
        "high" => "2",
        "medium" => "3",
        _ => "4"
    };

    private string MapPriorityToJiraPriority(string priority) => priority.ToLower() switch
    {
        "critical" => "Highest",
        "high" => "High",
        "medium" => "Medium",
        _ => "Low"
    };
}

public class TicketResult
{
    public string TicketId { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium"; // critical, high, medium, low
    public string? System { get; set; } // servicenow, jira, azure_devops
    public string? Category { get; set; }
    public string? AssignmentGroup { get; set; }
}

public class UpdateTicketRequest
{
    public string? Status { get; set; }
    public string? Comment { get; set; }
    public string? Assignee { get; set; }
}

internal class ServiceNowResponse
{
    public ServiceNowResult? result { get; set; }
}

internal class ServiceNowResult
{
    public string? sys_id { get; set; }
    public string? number { get; set; }
    public string? state { get; set; }
}

internal class JiraResponse
{
    public string? id { get; set; }
    public string? key { get; set; }
}

internal class AzureDevOpsResponse
{
    public int? id { get; set; }
}

