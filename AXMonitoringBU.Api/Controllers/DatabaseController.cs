using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/database")]
public class DatabaseController : ControllerBase
{
    private readonly IKpiDataService _kpiDataService;
    private readonly IBlockingService _blockingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        IKpiDataService kpiDataService,
        IBlockingService blockingService,
        IConfiguration configuration,
        ILogger<DatabaseController> logger)
    {
        _kpiDataService = kpiDataService;
        _blockingService = blockingService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("info")]
    public IActionResult GetDatabaseInfo()
    {
        try
        {
            var server = _configuration["Database:Server"] ?? "Unknown";
            var database = _configuration["Database:Name"] ?? "Unknown";
            var provider = _configuration["Database:Provider"] ?? "SqlServer";

            // Try to extract from connection string if not set directly
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if ((server == "Unknown" || database == "Unknown") && !string.IsNullOrEmpty(connectionString))
            {
                try
                {
                    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        // SQLite connection string format: "Data Source=filename.db"
                        if (connectionString.Contains("Data Source="))
                        {
                            var parts = connectionString.Split(';');
                            var dataSource = parts.FirstOrDefault(p => p.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
                            if (!string.IsNullOrEmpty(dataSource))
                            {
                                database = dataSource.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
                                server = "SQLite";
                            }
                        }
                    }
                    else
                    {
                        // SQL Server connection string
                        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                        if (!string.IsNullOrEmpty(builder.DataSource))
                            server = builder.DataSource;
                        if (!string.IsNullOrEmpty(builder.InitialCatalog))
                            database = builder.InitialCatalog;
                    }
                }
                catch
                {
                    // If parsing fails, keep original values
                }
            }

            var displayName = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase) 
                ? $"{database}" 
                : $"{server}\\{database}";

            // Get AOS servers
            var aosServers = _configuration.GetSection("AOS:Servers").Get<List<string>>() ?? new List<string>();
            var defaultAos = _configuration["AOS:DefaultServer"] ?? aosServers.FirstOrDefault() ?? "Unknown";

            return Ok(new
            {
                server = server,
                database = database,
                provider = provider,
                display_name = displayName,
                aos_servers = aosServers,
                default_aos = defaultAos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database info");
            return StatusCode(500, new { error = "Failed to retrieve database info" });
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            var sqlHealth = await _kpiDataService.GetSqlHealthAsync();

            return Ok(new
            {
                database_health = sqlHealth,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database health");
            return StatusCode(500, new { error = "Failed to retrieve database health" });
        }
    }

    [HttpGet("blocking")]
    public async Task<IActionResult> GetBlockingChains([FromQuery] bool activeOnly = true)
    {
        try
        {
            var blockingChains = await _blockingService.GetBlockingChainsAsync(activeOnly);

            return Ok(new
            {
                blocking_chains = blockingChains,
                count = blockingChains.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocking chains");
            return StatusCode(500, new { error = "Failed to retrieve blocking chains" });
        }
    }
}

