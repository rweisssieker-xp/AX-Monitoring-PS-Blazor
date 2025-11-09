using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Data.SqlClient;

namespace AXMonitoringBU.Api.Services;

public interface IDeadlockCaptureService
{
    Task EnsureSessionAsync(CancellationToken cancellationToken = default);
    Task<bool> IsSessionActiveAsync(CancellationToken cancellationToken = default);
    Task<List<DeadlockEvent>> GetDeadlocksFromSessionAsync(int topN = 100, CancellationToken cancellationToken = default);
}

public class DeadlockEvent
{
    public DateTime Timestamp { get; set; }
    public string DeadlockXml { get; set; } = string.Empty;
}

public class DeadlockCaptureService : IDeadlockCaptureService
{
    private const string DefaultSessionName = "AXMonitoring_DeadlockCapture";

    private readonly IConfiguration _configuration;
    private readonly ILogger<DeadlockCaptureService> _logger;
    private readonly string _connectionString;
    private readonly string _sessionName;
    private readonly string? _eventFilePath;

    public DeadlockCaptureService(
        IConfiguration configuration,
        ILogger<DeadlockCaptureService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured for Extended Events capture");

        _sessionName = _configuration["DeadlockCapture:SessionName"] ?? DefaultSessionName;
        _eventFilePath = _configuration["DeadlockCapture:EventFilePath"];
    }

    public async Task EnsureSessionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var exists = await SessionExistsAsync(connection, cancellationToken);
        if (!exists)
        {
            await CreateSessionAsync(connection, cancellationToken);
        }

        await StartSessionAsync(connection, cancellationToken);
    }

    public async Task<bool> IsSessionActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var query = "SELECT COUNT(*) FROM sys.dm_xe_sessions WHERE name = @sessionName";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add(new SqlParameter("@sessionName", SqlDbType.NVarChar, 128) { Value = _sessionName });

        var result = (int?)await command.ExecuteScalarAsync(cancellationToken) ?? 0;
        return result > 0;
    }

    public async Task<List<DeadlockEvent>> GetDeadlocksFromSessionAsync(int topN = 100, CancellationToken cancellationToken = default)
    {
        var events = new List<DeadlockEvent>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var ringBufferSql = @"
SELECT TOP (@topN)
    DATEADD(mi, DATEDIFF(mi, SYSUTCDATETIME(), SYSDATETIME()), 
        xevent.value('@timestamp','datetime2')) AS event_time,
    CONVERT(nvarchar(max), xevent.query('.')) AS event_xml
FROM (
    SELECT CAST(target_data AS XML) AS target_data
    FROM sys.dm_xe_session_targets st
    INNER JOIN sys.dm_xe_sessions s ON s.address = st.event_session_address
    WHERE s.name = @sessionName AND st.target_name = 'ring_buffer'
) AS session_data
CROSS APPLY session_data.target_data.nodes('RingBufferTarget/event') AS ev(xevent)
WHERE xevent.value('@name','nvarchar(128)') = 'xml_deadlock_report'
ORDER BY event_time DESC";

        await using (var command = new SqlCommand(ringBufferSql, connection))
        {
            command.Parameters.Add(new SqlParameter("@sessionName", SqlDbType.NVarChar, 128) { Value = _sessionName });
            command.Parameters.Add(new SqlParameter("@topN", SqlDbType.Int) { Value = topN });
            command.CommandTimeout = 30;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    var timestamp = reader.IsDBNull(0) ? DateTime.UtcNow : reader.GetDateTime(0);
                    var eventXml = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                    if (string.IsNullOrWhiteSpace(eventXml))
                    {
                        continue;
                    }

                    // Ensure we return only the deadlock report payload
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(eventXml);

                    var deadlockNode = xmlDoc.SelectSingleNode("/event/data/value/deadlock");
                    if (deadlockNode == null)
                    {
                        deadlockNode = xmlDoc.SelectSingleNode("/event/data/value/xml_deadlock_report/deadlock");
                    }

                    string deadlockPayload;
                    if (deadlockNode != null)
                    {
                        deadlockPayload = deadlockNode.OuterXml;
                    }
                    else
                    {
                        deadlockPayload = eventXml;
                    }

                    events.Add(new DeadlockEvent
                    {
                        Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                        DeadlockXml = deadlockPayload
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse deadlock event from Extended Events session");
                }
            }
        }

        // If no data found in ring buffer and an event file is configured, try reading from file target as fallback
        if (events.Count == 0 && !string.IsNullOrWhiteSpace(_eventFilePath))
        {
            var fileSql = $@"
SELECT TOP (@topN)
    xevent.value('(event/@timestamp)[1]', 'datetime2') AS event_time,
    CONVERT(nvarchar(max), xevent.query('.')) AS event_xml
FROM sys.fn_xe_file_target_read_file(@filePattern, NULL, NULL, NULL) AS xe
CROSS APPLY CAST(event_data AS XML).nodes('/event') AS t(xevent)
WHERE xevent.value('@name', 'nvarchar(128)') = 'xml_deadlock_report'
ORDER BY event_time DESC";

            var pattern = _eventFilePath;
            if (!pattern.EndsWith(".xel", StringComparison.OrdinalIgnoreCase))
            {
                pattern = pattern.TrimEnd('\n', '\r', '\t', ' ');
                if (!pattern.EndsWith("deadlock", StringComparison.OrdinalIgnoreCase))
                {
                    pattern = Path.Combine(pattern, "deadlock");
                }
                pattern += "*.xel";
            }

            await using var fileCommand = new SqlCommand(fileSql, connection);
            fileCommand.Parameters.Add(new SqlParameter("@filePattern", SqlDbType.NVarChar, 4000) { Value = pattern });
            fileCommand.Parameters.Add(new SqlParameter("@topN", SqlDbType.Int) { Value = topN });
            fileCommand.CommandTimeout = 60;

            await using var reader = await fileCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                try
                {
                    var timestamp = reader.IsDBNull(0) ? DateTime.UtcNow : reader.GetDateTime(0);
                    var eventXml = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    if (string.IsNullOrWhiteSpace(eventXml))
                    {
                        continue;
                    }

                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(eventXml);

                    var deadlockNode = xmlDoc.SelectSingleNode("/event/data/value/deadlock");
                    if (deadlockNode == null)
                    {
                        deadlockNode = xmlDoc.SelectSingleNode("/event/data/value/xml_deadlock_report/deadlock");
                    }

                    string deadlockPayload = deadlockNode != null
                        ? deadlockNode.OuterXml
                        : eventXml;

                    events.Add(new DeadlockEvent
                    {
                        Timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
                        DeadlockXml = deadlockPayload
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse deadlock event from Extended Events file target");
                }
            }
        }

        return events;
    }

    private async Task<bool> SessionExistsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string query = "SELECT COUNT(*) FROM sys.server_event_sessions WHERE name = @session";
        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add(new SqlParameter("@session", SqlDbType.NVarChar, 128) { Value = _sessionName });

        var result = (int?)await command.ExecuteScalarAsync(cancellationToken) ?? 0;
        return result > 0;
    }

    private async Task CreateSessionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"CREATE EVENT SESSION [{_sessionName}] ON SERVER");
        builder.AppendLine("ADD EVENT sqlserver.xml_deadlock_report(");
        builder.AppendLine("    ACTION(\n        sqlserver.sql_text,\n        sqlserver.session_id,\n        sqlserver.database_id,\n        sqlserver.client_app_name,\n        sqlserver.username,\n        sqlserver.client_hostname\n    )\n)");
        builder.AppendLine("ADD TARGET package0.ring_buffer");

        if (!string.IsNullOrWhiteSpace(_eventFilePath))
        {
            var normalizedPath = _eventFilePath.Replace("'", "''");
            builder.AppendLine("ADD TARGET package0.event_file(SET filename = N'" + normalizedPath + "', max_file_size = (25), max_rollover_files = (5))");
        }

        builder.AppendLine("WITH (\n    MAX_MEMORY = 4096 KB,\n    EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS,\n    MAX_DISPATCH_LATENCY = 5 SECONDS,\n    TRACK_CAUSALITY = ON,\n    STARTUP_STATE = ON\n);");

        var createSql = builder.ToString();

        await using var command = new SqlCommand(createSql, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 60
        };

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Created Extended Events session {SessionName} for deadlock capture", _sessionName);
    }

    private async Task StartSessionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var startSql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.dm_xe_sessions WHERE name = @sessionName)
    BEGIN
        ALTER EVENT SESSION [{_sessionName}] ON SERVER STATE = START;
    END";

        await using var command = new SqlCommand(startSql, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 30
        };

        command.Parameters.Add(new SqlParameter("@sessionName", SqlDbType.NVarChar, 128) { Value = _sessionName });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
