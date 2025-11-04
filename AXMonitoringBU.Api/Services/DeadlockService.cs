using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Xml.Linq;

namespace AXMonitoringBU.Api.Services;

public interface IDeadlockService
{
    Task<List<DeadlockInfo>> GetRecentDeadlocksAsync(int count = 100);
    Task<DeadlockInfo?> GetDeadlockByIdAsync(string deadlockId);
    Task<int> GetDeadlockCountAsync(DateTime? since = null);
}

public class DeadlockInfo
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string VictimSessionId { get; set; } = string.Empty;
    public string DeadlockXml { get; set; } = string.Empty;
    public List<DeadlockProcess> Processes { get; set; } = new();
    public List<DeadlockResource> Resources { get; set; } = new();
}

public class DeadlockProcess
{
    public string ProcessId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public string LockMode { get; set; } = string.Empty;
    public bool IsVictim { get; set; }
}

public class DeadlockResource
{
    public string ResourceType { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string AssociatedObjectId { get; set; } = string.Empty;
}

public class DeadlockService : IDeadlockService
{
    private readonly AXDbContext _context;
    private readonly ILogger<DeadlockService> _logger;
    private readonly IConfiguration _configuration;

    public DeadlockService(
        AXDbContext context,
        ILogger<DeadlockService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<List<DeadlockInfo>> GetRecentDeadlocksAsync(int count = 100)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Database connection string not configured");

            var deadlocks = new List<DeadlockInfo>();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Check if view exists, otherwise try direct file access
            var viewName = _configuration["Database:DeadlockView"] ?? "dbo.v_Deadlocks";
            var xePath = _configuration["Database:ExtendedEventsPath"];

            string query;
            if (!string.IsNullOrEmpty(xePath))
            {
                // Direct file access query
                query = $@"
                    WITH src AS (
                        SELECT CAST(event_data AS XML) AS x
                        FROM sys.fn_xe_file_target_read_file(N'{xePath}\\deadlock*.xel', NULL, NULL, NULL)
                    )
                    SELECT TOP {count}
                        x.value('(event/@timestamp)[1]','datetime2') AS event_ts,
                        x.value('(event/data/value/deadlock/@victim)[1]','varchar(10)') AS victim,
                        CAST(x.query('(event/data/value/deadlock)[1]') AS VARCHAR(MAX)) AS deadlock_xml,
                        CAST(x AS VARCHAR(MAX)) AS full_xml
                    FROM src
                    WHERE x.value('(event/@timestamp)[1]','datetime2') IS NOT NULL
                    ORDER BY event_ts DESC";
            }
            else
            {
                // Use view if available
                query = $@"
                    SELECT TOP {count}
                        event_ts,
                        victim,
                        CAST(event_xml AS VARCHAR(MAX)) AS deadlock_xml,
                        CAST(event_xml AS VARCHAR(MAX)) AS full_xml
                    FROM {viewName}
                    WHERE event_ts IS NOT NULL
                    ORDER BY event_ts DESC";
            }

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    var timestamp = reader.GetDateTime(0);
                    var victim = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var deadlockXml = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    var fullXml = reader.IsDBNull(3) ? "" : reader.GetString(3);

                    var deadlock = ParseDeadlockXml(timestamp, victim, deadlockXml, fullXml);
                    if (deadlock != null)
                    {
                        deadlocks.Add(deadlock);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing deadlock record");
                }
            }

            _logger.LogInformation("Retrieved {Count} deadlocks", deadlocks.Count);
            return deadlocks;
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name
        {
            _logger.LogWarning("Deadlock view or Extended Events path not configured. Please configure Database:DeadlockView or Database:ExtendedEventsPath");
            return new List<DeadlockInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deadlocks");
            throw;
        }
    }

    public async Task<DeadlockInfo?> GetDeadlockByIdAsync(string deadlockId)
    {
        try
        {
            var deadlocks = await GetRecentDeadlocksAsync(1000);
            return deadlocks.FirstOrDefault(d => d.Id == deadlockId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlock by id {DeadlockId}", deadlockId);
            throw;
        }
    }

    public async Task<int> GetDeadlockCountAsync(DateTime? since = null)
    {
        try
        {
            var deadlocks = await GetRecentDeadlocksAsync(10000);
            if (since.HasValue)
            {
                return deadlocks.Count(d => d.Timestamp >= since.Value);
            }
            return deadlocks.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deadlock count");
            return 0;
        }
    }

    private DeadlockInfo? ParseDeadlockXml(DateTime timestamp, string victim, string deadlockXml, string fullXml)
    {
        try
        {
            if (string.IsNullOrEmpty(deadlockXml) && string.IsNullOrEmpty(fullXml))
                return null;

            var xml = !string.IsNullOrEmpty(deadlockXml) ? deadlockXml : fullXml;
            var doc = XDocument.Parse(xml);

            var deadlock = doc.Descendants("deadlock").FirstOrDefault();
            if (deadlock == null)
                return null;

            var deadlockInfo = new DeadlockInfo
            {
                Id = $"DL_{timestamp:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}",
                Timestamp = timestamp,
                VictimSessionId = victim,
                DeadlockXml = xml
            };

            // Parse processes
            foreach (var process in deadlock.Descendants("process"))
            {
                var processId = process.Attribute("id")?.Value ?? "";
                var sessionId = process.Attribute("taskpriority")?.Value ?? "";
                var dbName = process.Element("executionStack")?.Element("frame")?.Attribute("database")?.Value ?? "";
                var sqlText = process.Element("executionStack")?.Element("frame")?.Element("sqlhandle")?.Value ?? "";

                deadlockInfo.Processes.Add(new DeadlockProcess
                {
                    ProcessId = processId,
                    SessionId = sessionId,
                    DatabaseName = dbName,
                    SqlText = sqlText,
                    IsVictim = sessionId == victim
                });
            }

            // Parse resources
            foreach (var resource in deadlock.Descendants("resource-list").Elements())
            {
                var objectName = resource.Element("object")?.Attribute("name")?.Value ?? "";
                var indexName = resource.Element("index")?.Attribute("name")?.Value ?? "";

                deadlockInfo.Resources.Add(new DeadlockResource
                {
                    ResourceType = resource.Name.LocalName,
                    ObjectName = objectName,
                    IndexName = indexName,
                    AssociatedObjectId = resource.Attribute("id")?.Value ?? ""
                });
            }

            return deadlockInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing deadlock XML");
            return null;
        }
    }
}

