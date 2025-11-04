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
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:ServiceNow:BaseUrl"];
            var username = _configuration["Integrations:Ticketing:ServiceNow:Username"];
            var password = _configuration["Integrations:Ticketing:ServiceNow:Password"];
            var table = _configuration["Integrations:Ticketing:ServiceNow:Table"] ?? "incident";

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("ServiceNow configuration is missing");
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{baseUrl}/api/now/table/{table}/{ticketId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve ServiceNow ticket {TicketId}: {StatusCode}", ticketId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceNowResponse>(cancellationToken: cancellationToken);
            
            return result?.result != null ? new TicketResult
            {
                TicketId = result.result.sys_id ?? ticketId,
                ExternalId = result.result.number ?? ticketId,
                System = "servicenow",
                Status = result.result.state ?? "Unknown",
                CreatedAt = DateTime.UtcNow
            } : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ServiceNow ticket {TicketId}", ticketId);
            return null;
        }
    }

    private async Task<TicketResult?> GetJiraTicketAsync(string ticketId, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:Jira:BaseUrl"];
            var username = _configuration["Integrations:Ticketing:Jira:Username"];
            var apiToken = _configuration["Integrations:Ticketing:Jira:ApiToken"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(apiToken))
            {
                _logger.LogWarning("Jira configuration is missing");
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{baseUrl}/rest/api/3/issue/{ticketId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve Jira ticket {TicketId}: {StatusCode}", ticketId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<JiraIssueResponse>(cancellationToken: cancellationToken);
            
            return result != null ? new TicketResult
            {
                TicketId = result.id ?? ticketId,
                ExternalId = result.key ?? ticketId,
                System = "jira",
                Status = result.fields?.status?.name ?? "Unknown",
                CreatedAt = result.fields?.created != null ? DateTime.Parse(result.fields.created) : DateTime.UtcNow
            } : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Jira ticket {TicketId}", ticketId);
            return null;
        }
    }

    private async Task<TicketResult?> GetAzureDevOpsTicketAsync(string ticketId, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:AzureDevOps:BaseUrl"];
            var pat = _configuration["Integrations:Ticketing:AzureDevOps:PersonalAccessToken"];
            var organization = _configuration["Integrations:Ticketing:AzureDevOps:Organization"];
            var project = _configuration["Integrations:Ticketing:AzureDevOps:Project"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
            {
                _logger.LogWarning("Azure DevOps configuration is missing");
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}")));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(
                $"{baseUrl}/{organization}/{project}/_apis/wit/workitems/{ticketId}?api-version=6.0", 
                cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve Azure DevOps ticket {TicketId}: {StatusCode}", ticketId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AzureDevOpsWorkItemResponse>(cancellationToken: cancellationToken);
            
            return result != null ? new TicketResult
            {
                TicketId = result.id?.ToString() ?? ticketId,
                ExternalId = result.id?.ToString() ?? ticketId,
                System = "azure_devops",
                Status = result.fields?.GetValueOrDefault("System.State")?.ToString() ?? "Unknown",
                CreatedAt = result.fields?.GetValueOrDefault("System.CreatedDate") != null 
                    ? DateTime.Parse(result.fields["System.CreatedDate"].ToString() ?? "") 
                    : DateTime.UtcNow
            } : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Azure DevOps ticket {TicketId}", ticketId);
            return null;
        }
    }

    private async Task<bool> UpdateServiceNowTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:ServiceNow:BaseUrl"];
            var username = _configuration["Integrations:Ticketing:ServiceNow:Username"];
            var password = _configuration["Integrations:Ticketing:ServiceNow:Password"];
            var table = _configuration["Integrations:Ticketing:ServiceNow:Table"] ?? "incident";

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("ServiceNow configuration is missing");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                payload["state"] = request.Status;
            }
            if (!string.IsNullOrEmpty(request.Comment))
            {
                payload["comments"] = request.Comment;
            }
            if (!string.IsNullOrEmpty(request.Assignee))
            {
                payload["assigned_to"] = request.Assignee;
            }

            if (payload.Count == 0)
            {
                return false;
            }

            var response = await client.PatchAsJsonAsync($"{baseUrl}/api/now/table/{table}/{ticketId}", payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ServiceNow ticket {TicketId}", ticketId);
            return false;
        }
    }

    private async Task<bool> UpdateJiraTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:Jira:BaseUrl"];
            var username = _configuration["Integrations:Ticketing:Jira:Username"];
            var apiToken = _configuration["Integrations:Ticketing:Jira:ApiToken"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(apiToken))
            {
                _logger.LogWarning("Jira configuration is missing");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var updateFields = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                updateFields["status"] = new { name = request.Status };
            }
            if (!string.IsNullOrEmpty(request.Assignee))
            {
                updateFields["assignee"] = new { name = request.Assignee };
            }

            var payload = new
            {
                fields = updateFields
            };

            var response = await client.PutAsJsonAsync($"{baseUrl}/rest/api/3/issue/{ticketId}", payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Jira ticket {TicketId}", ticketId);
            return false;
        }
    }

    private async Task<bool> UpdateAzureDevOpsTicketAsync(string ticketId, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:AzureDevOps:BaseUrl"];
            var pat = _configuration["Integrations:Ticketing:AzureDevOps:PersonalAccessToken"];
            var organization = _configuration["Integrations:Ticketing:AzureDevOps:Organization"];
            var project = _configuration["Integrations:Ticketing:AzureDevOps:Project"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
            {
                _logger.LogWarning("Azure DevOps configuration is missing");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}")));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var operations = new List<object>();
            if (!string.IsNullOrEmpty(request.Status))
            {
                operations.Add(new { op = "replace", path = "/fields/System.State", value = request.Status });
            }
            if (!string.IsNullOrEmpty(request.Comment))
            {
                operations.Add(new { op = "add", path = "/fields/System.History", value = request.Comment });
            }
            if (!string.IsNullOrEmpty(request.Assignee))
            {
                operations.Add(new { op = "replace", path = "/fields/System.AssignedTo", value = request.Assignee });
            }

            if (operations.Count == 0)
            {
                return false;
            }

            var response = await client.PatchAsJsonAsync(
                $"{baseUrl}/{organization}/{project}/_apis/wit/workitems/{ticketId}?api-version=6.0",
                operations,
                cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Azure DevOps ticket {TicketId}", ticketId);
            return false;
        }
    }

    private async Task<List<TicketResult>> GetServiceNowTicketsAsync(string? status, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:ServiceNow:BaseUrl"];
            var username = _configuration["Integrations:Ticketing:ServiceNow:Username"];
            var password = _configuration["Integrations:Ticketing:ServiceNow:Password"];
            var table = _configuration["Integrations:Ticketing:ServiceNow:Table"] ?? "incident";

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("ServiceNow configuration is missing");
                return new List<TicketResult>();
            }

            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var query = string.IsNullOrEmpty(status) 
                ? $"{baseUrl}/api/now/table/{table}?sysparm_limit=100"
                : $"{baseUrl}/api/now/table/{table}?sysparm_limit=100&state={status}";

            var response = await client.GetAsync(query, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve ServiceNow tickets: {StatusCode}", response.StatusCode);
                return new List<TicketResult>();
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceNowListResponse>(cancellationToken: cancellationToken);
            
            return result?.result?.Select(r => new TicketResult
            {
                TicketId = r.sys_id ?? "",
                ExternalId = r.number ?? "",
                System = "servicenow",
                Status = r.state ?? "Unknown",
                CreatedAt = r.sys_created_on != null ? DateTime.Parse(r.sys_created_on) : DateTime.UtcNow
            }).ToList() ?? new List<TicketResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ServiceNow tickets");
            return new List<TicketResult>();
        }
    }

    private async Task<List<TicketResult>> GetJiraTicketsAsync(string? status, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:Jira:BaseUrl"];
            var username = _configuration["Integrations:Ticketing:Jira:Username"];
            var apiToken = _configuration["Integrations:Ticketing:Jira:ApiToken"];
            var projectKey = _configuration["Integrations:Ticketing:Jira:ProjectKey"] ?? "MON";

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(apiToken))
            {
                _logger.LogWarning("Jira configuration is missing");
                return new List<TicketResult>();
            }

            var client = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var jql = string.IsNullOrEmpty(status)
                ? $"project = {projectKey} ORDER BY created DESC"
                : $"project = {projectKey} AND status = \"{status}\" ORDER BY created DESC";

            var query = $"{baseUrl}/rest/api/3/search?jql={Uri.EscapeDataString(jql)}&maxResults=100";

            var response = await client.GetAsync(query, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve Jira tickets: {StatusCode}", response.StatusCode);
                return new List<TicketResult>();
            }

            var result = await response.Content.ReadFromJsonAsync<JiraSearchResponse>(cancellationToken: cancellationToken);
            
            return result?.issues?.Select(i => new TicketResult
            {
                TicketId = i.id ?? "",
                ExternalId = i.key ?? "",
                System = "jira",
                Status = i.fields?.status?.name ?? "Unknown",
                CreatedAt = i.fields?.created != null ? DateTime.Parse(i.fields.created) : DateTime.UtcNow
            }).ToList() ?? new List<TicketResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Jira tickets");
            return new List<TicketResult>();
        }
    }

    private async Task<List<TicketResult>> GetAzureDevOpsTicketsAsync(string? status, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Integrations:Ticketing:AzureDevOps:BaseUrl"];
            var pat = _configuration["Integrations:Ticketing:AzureDevOps:PersonalAccessToken"];
            var organization = _configuration["Integrations:Ticketing:AzureDevOps:Organization"];
            var project = _configuration["Integrations:Ticketing:AzureDevOps:Project"];

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(pat) || string.IsNullOrEmpty(organization) || string.IsNullOrEmpty(project))
            {
                _logger.LogWarning("Azure DevOps configuration is missing");
                return new List<TicketResult>();
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.UTF8.GetBytes($":{pat}")));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var wiql = string.IsNullOrEmpty(status)
                ? "SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Incident' ORDER BY [System.CreatedDate] DESC"
                : $"SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Incident' AND [System.State] = '{status}' ORDER BY [System.CreatedDate] DESC";

            var wiqlPayload = new { query = wiql };
            var wiqlResponse = await client.PostAsJsonAsync(
                $"{baseUrl}/{organization}/{project}/_apis/wit/wiql?api-version=6.0",
                wiqlPayload,
                cancellationToken);

            if (!wiqlResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to query Azure DevOps tickets: {StatusCode}", wiqlResponse.StatusCode);
                return new List<TicketResult>();
            }

            var wiqlResult = await wiqlResponse.Content.ReadFromJsonAsync<AzureDevOpsWiqlResponse>(cancellationToken: cancellationToken);
            
            if (wiqlResult?.workItems == null || !wiqlResult.workItems.Any())
            {
                return new List<TicketResult>();
            }

            var workItemIds = string.Join(",", wiqlResult.workItems.Select(wi => wi.id));
            var workItemsResponse = await client.GetAsync(
                $"{baseUrl}/{organization}/{project}/_apis/wit/workitems?ids={workItemIds}&api-version=6.0",
                cancellationToken);

            if (!workItemsResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to retrieve Azure DevOps work items: {StatusCode}", workItemsResponse.StatusCode);
                return new List<TicketResult>();
            }

            var workItemsResult = await workItemsResponse.Content.ReadFromJsonAsync<AzureDevOpsWorkItemsResponse>(cancellationToken: cancellationToken);
            
            return workItemsResult?.value?.Select(wi => new TicketResult
            {
                TicketId = wi.id?.ToString() ?? "",
                ExternalId = wi.id?.ToString() ?? "",
                System = "azure_devops",
                Status = wi.fields?.GetValueOrDefault("System.State")?.ToString() ?? "Unknown",
                CreatedAt = wi.fields?.GetValueOrDefault("System.CreatedDate") != null
                    ? DateTime.Parse(wi.fields["System.CreatedDate"].ToString() ?? "")
                    : DateTime.UtcNow
            }).ToList() ?? new List<TicketResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Azure DevOps tickets");
            return new List<TicketResult>();
        }
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
    public string? sys_created_on { get; set; }
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

internal class JiraIssueResponse
{
    public string? id { get; set; }
    public string? key { get; set; }
    public JiraIssueFields? fields { get; set; }
}

internal class JiraIssueFields
{
    public JiraStatus? status { get; set; }
    public string? created { get; set; }
}

internal class JiraStatus
{
    public string? name { get; set; }
}

internal class JiraSearchResponse
{
    public List<JiraIssue>? issues { get; set; }
}

internal class JiraIssue
{
    public string? id { get; set; }
    public string? key { get; set; }
    public JiraIssueFields? fields { get; set; }
}

internal class AzureDevOpsWorkItemResponse
{
    public int? id { get; set; }
    public Dictionary<string, object>? fields { get; set; }
}

internal class AzureDevOpsWorkItemsResponse
{
    public List<AzureDevOpsWorkItemResponse>? value { get; set; }
}

internal class AzureDevOpsWiqlResponse
{
    public List<AzureDevOpsWorkItemReference>? workItems { get; set; }
}

internal class AzureDevOpsWorkItemReference
{
    public int? id { get; set; }
}

internal class ServiceNowListResponse
{
    public List<ServiceNowResult>? result { get; set; }
}

