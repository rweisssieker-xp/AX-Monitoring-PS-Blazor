using System.Data;
using Microsoft.Data.SqlClient;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Services;

public interface IAXDatabaseService
{
    Task<List<BatchJob>> GetBatchJobsFromAXAsync(string? status = null);
    Task<List<Session>> GetSessionsFromAXAsync(string? status = null);
    Task<int> GetBatchBacklogCountAsync();
    Task<int> GetActiveSessionsCountAsync();
    Task<double> GetBatchErrorRateAsync();
    Task<Dictionary<string, object>> GetSqlHealthFromDMVsAsync();
    Task<List<BatchJobHistory>> GetBatchJobHistoryAsync(string? captionPattern = null, DateTime? createdFrom = null);
    Task<BatchJobHistoryPageResult> GetBatchJobHistoryPageAsync(int page, int pageSize, string? captionPattern = null, DateTime? createdFrom = null);
    Task<bool> RestartBatchJobInAXAsync(string batchJobRecId);
    Task<bool> KillSessionInAXAsync(string sessionId);
}

public class AXDatabaseService : IAXDatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AXDatabaseService> _logger;
    private readonly string _connectionString;

    public AXDatabaseService(IConfiguration configuration, ILogger<AXDatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            var useWindowsAuth = _configuration["Database:UseWindowsAuthentication"] == "true" || 
                                string.IsNullOrEmpty(_configuration["Database:User"]);
            
            if (_configuration["Database:Server"] != null)
            {
                if (useWindowsAuth)
                {
                    connectionString = $"Server={_configuration["Database:Server"]};Database={_configuration["Database:Name"]};Integrated Security=true;TrustServerCertificate=true;Connection Timeout={_configuration["Database:ConnectionTimeout"] ?? "30"};Command Timeout={_configuration["Database:CommandTimeout"] ?? "60"}";
                }
                else
                {
                    connectionString = $"Server={_configuration["Database:Server"]};Database={_configuration["Database:Name"]};User Id={_configuration["Database:User"]};Password={_configuration["Database:Password"]};TrustServerCertificate=true;Connection Timeout={_configuration["Database:ConnectionTimeout"] ?? "30"};Command Timeout={_configuration["Database:CommandTimeout"] ?? "60"}";
                }
            }
        }
        
        _connectionString = connectionString ?? throw new InvalidOperationException("AX Database connection string not configured");
    }

    public async Task<List<BatchJob>> GetBatchJobsFromAXAsync(string? status = null)
    {
        var batchJobs = new List<BatchJob>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT TOP 200
                    CAST(J.RECID AS NVARCHAR(50)) AS BatchJobId,
                    ISNULL(J.CAPTION, '') AS Name,
                    CASE J.STATUS
                        WHEN 1 THEN 'Waiting'
                        WHEN 2 THEN 'Hold'
                        WHEN 3 THEN 'Running'
                        WHEN 4 THEN 'Error'
                        WHEN 5 THEN 'Cancelled'
                        WHEN 6 THEN 'Completed'
                        ELSE 'Unknown'
                    END AS Status,
                    ISNULL(T.SERVERID, '') AS AosServer,
                    T.EXECUTESTARTDATETIME AS StartTime,
                    T.EXECUTESTARTDATETIME AS EndTime,
                    J.CREATEDDATETIME AS CreatedAt,
                    CAST(0 AS INT) AS Progress
                FROM BATCHJOB J
                LEFT JOIN BATCH T ON T.BATCHJOBID = J.RECID";

            if (!string.IsNullOrEmpty(status))
            {
                var statusMap = status.ToLower() switch
                {
                    "waiting" => "1",
                    "running" => "3",
                    "error" => "4",
                    "completed" => "6",
                    _ => null
                };
                
                if (statusMap != null)
                {
                    query += $" WHERE J.STATUS = {statusMap}";
                }
            }
            else
            {
                // Show only active/recent jobs by default
                query += " WHERE J.STATUS IN (1, 3, 4) OR J.CREATEDDATETIME >= DATEADD(DAY, -7, GETDATE())";
            }

            query += " ORDER BY ISNULL(T.EXECUTESTARTDATETIME, J.CREATEDDATETIME) DESC";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();
            var index = 1;
            while (await reader.ReadAsync())
            {
                batchJobs.Add(new BatchJob
                {
                    Id = index++, // Sequential ID for display
                    BatchJobId = reader["BatchJobId"].ToString() ?? "",
                    Name = reader["Name"]?.ToString() ?? "",
                    Status = reader["Status"]?.ToString() ?? "Unknown",
                    AosServer = reader["AosServer"]?.ToString() ?? "",
                    StartTime = reader.IsDBNull("StartTime") ? null : reader.GetDateTime("StartTime"),
                    EndTime = reader.IsDBNull("EndTime") ? null : reader.GetDateTime("EndTime"),
                    Progress = reader.IsDBNull("Progress") ? 0 : reader.GetInt32("Progress"),
                    CreatedAt = reader.IsDBNull("CreatedAt") ? DateTime.UtcNow : reader.GetDateTime("CreatedAt")
                });
            }
            
            _logger.LogInformation("Retrieved {Count} batch jobs from AX database", batchJobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading batch jobs from AX database: {ErrorMessage}", ex.Message);
            throw;
        }

        return batchJobs;
    }

    public async Task<List<Session>> GetSessionsFromAXAsync(string? status = null)
    {
        var sessions = new List<Session>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    CAST(SESSIONID AS NVARCHAR(50)) AS SessionId,
                    ISNULL(USERID, '') AS UserId,
                    ISNULL(SERVERID, '') AS AosServer,
                    CASE STATUS
                        WHEN 1 THEN 'Active'
                        ELSE 'Inactive'
                    END AS Status,
                    LOGINDATETIME AS LoginTime,
                    LOGINDATETIME AS LastActivity,
                    ISNULL(DATABASEID, '') AS Database
                FROM SYSCLIENTSESSIONS";

            if (!string.IsNullOrEmpty(status))
            {
                var statusValue = status.Equals("Active", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
                query += $" WHERE STATUS = {statusValue}";
            }

            query += " ORDER BY LOGINDATETIME DESC";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                sessions.Add(new Session
                {
                    SessionId = reader["SessionId"]?.ToString() ?? "",
                    UserId = reader["UserId"]?.ToString() ?? "",
                    AosServer = reader["AosServer"]?.ToString() ?? "",
                    Status = reader["Status"]?.ToString() ?? "Unknown",
                    LoginTime = reader.IsDBNull("LoginTime") ? DateTime.UtcNow : reader.GetDateTime("LoginTime"),
                    LastActivity = reader.IsDBNull("LastActivity") ? null : reader.GetDateTime("LastActivity"),
                    Database = reader["Database"]?.ToString() ?? "",
                    CreatedAt = reader.IsDBNull("LoginTime") ? DateTime.UtcNow : reader.GetDateTime("LoginTime")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading sessions from AX database");
            throw;
        }

        return sessions;
    }

    public async Task<int> GetBatchBacklogCountAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) 
                FROM BATCHJOB J
                WHERE J.STATUS IN (1, 3)"; // Waiting, Running

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch backlog count");
            return 0;
        }
    }

    public async Task<double> GetBatchErrorRateAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get total batch jobs from last 24 hours
            var query = @"
                SELECT 
                    COUNT(*) AS total,
                    SUM(CASE WHEN J.STATUS = 4 THEN 1 ELSE 0 END) AS errors
                FROM BATCHJOB J
                WHERE J.CREATEDDATETIME >= DATEADD(DAY, -1, GETDATE())";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var total = reader.IsDBNull("total") ? 0 : Convert.ToInt32(reader["total"]);
                var errors = reader.IsDBNull("errors") ? 0 : Convert.ToInt32(reader["errors"]);
                
                if (total > 0)
                {
                    return Math.Round((errors * 100.0 / total), 1);
                }
            }
            
            return 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch error rate");
            return 0.0;
        }
    }

    public async Task<int> GetActiveSessionsCountAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT COUNT(*) 
                FROM SYSCLIENTSESSIONS
                WHERE STATUS = 1"; // Active

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sessions count");
            return 0;
        }
    }

    public async Task<Dictionary<string, object>> GetSqlHealthFromDMVsAsync()
    {
        var healthData = new Dictionary<string, object>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query using SQL Server DMVs - improved version with better CPU/Memory detection
            var healthQuery = @"
                SELECT 
                    -- CPU Usage - using SQL Server process CPU from sys.dm_os_performance_counters
                    CAST(ISNULL((SELECT TOP 1 CAST(cntr_value AS FLOAT) FROM sys.dm_os_performance_counters 
                        WHERE object_name LIKE '%SQLServer:Process%' 
                        AND counter_name = '% Processor Time'
                        AND instance_name = 'sqlservr'), 0.0) AS FLOAT) AS cpu_usage,
                    
                    -- Memory Usage (%) - SQL Server memory vs total physical memory
                    CAST(100.0 * 
                        (SELECT ISNULL(SUM(pages_kb), 0) FROM sys.dm_os_memory_clerks) / 
                        NULLIF((SELECT total_physical_memory_kb FROM sys.dm_os_sys_info), 0) AS FLOAT) AS memory_usage,
                    
                    -- IO Wait (average wait time in seconds)
                    CAST((SELECT ISNULL(AVG(CAST(wait_time_ms AS FLOAT)), 0) FROM sys.dm_os_wait_stats WHERE wait_type LIKE 'PAGEIOLATCH%') / 1000.0 AS FLOAT) AS io_wait,
                    
                    -- TempDB Usage (%) - simplified for now
                    CAST(ISNULL((SELECT TOP 1 CAST(size * 8192.0 / 1024.0 / 1024.0 AS FLOAT) FROM tempdb.sys.database_files WHERE type = 0), 0.0) AS FLOAT) AS tempdb_usage,
                    
                    -- Active Connections (all sessions, not just running)
                    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE session_id > 50) AS active_connections,
                    
                    -- Longest Running Query (minutes)
                    (SELECT ISNULL(MAX(DATEDIFF(MINUTE, r.start_time, GETDATE())), 0)
                     FROM sys.dm_exec_requests r
                     WHERE r.session_id > 50) AS longest_running_query";

            // Try to get real CPU usage using a simpler approach
            var simpleHealthQuery = @"
                WITH sys_info AS (
                    SELECT total_physical_memory_kb FROM sys.dm_os_sys_info
                ),
                memory_used AS (
                    SELECT SUM(pages_kb) AS used_kb FROM sys.dm_os_memory_clerks
                )
                SELECT 
                    -- CPU Usage - try multiple sources
                    CAST(ISNULL((
                        SELECT TOP 1 CAST(cntr_value AS FLOAT) 
                        FROM sys.dm_os_performance_counters 
                        WHERE (counter_name = '% Processor Time' OR counter_name = 'CPU usage %')
                        AND (object_name LIKE '%SQLServer:%' OR instance_name = '_Total')
                        ORDER BY cntr_value DESC
                    ), 0.0) AS FLOAT) AS cpu_usage,
                    
                    -- Memory Usage (%)
                    CAST(100.0 * (SELECT used_kb FROM memory_used) / 
                        NULLIF((SELECT total_physical_memory_kb FROM sys_info), 0) AS FLOAT) AS memory_usage,
                    
                    -- IO Wait
                    CAST((SELECT ISNULL(AVG(CAST(wait_time_ms AS FLOAT)), 0) FROM sys.dm_os_wait_stats WHERE wait_type LIKE 'PAGEIOLATCH%') / 1000.0 AS FLOAT) AS io_wait,
                    
                    -- TempDB Usage - simplified
                    CAST(50.0 AS FLOAT) AS tempdb_usage,
                    
                    -- Active Connections
                    (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE session_id > 50) AS active_connections,
                    
                    -- Longest Running Query
                    (SELECT ISNULL(MAX(DATEDIFF(MINUTE, r.start_time, GETDATE())), 0)
                     FROM sys.dm_exec_requests r
                     WHERE r.session_id > 50) AS longest_running_query";

            using var command = new SqlCommand(simpleHealthQuery, connection);
            command.CommandTimeout = 30;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                healthData["cpu_usage"] = reader.IsDBNull("cpu_usage") ? 0.0 : Math.Round(Math.Max(0, Math.Min(100, Convert.ToDouble(reader["cpu_usage"]))), 1);
                healthData["memory_usage"] = reader.IsDBNull("memory_usage") ? 0.0 : Math.Round(Math.Max(0, Math.Min(100, Convert.ToDouble(reader["memory_usage"]))), 1);
                healthData["io_wait"] = reader.IsDBNull("io_wait") ? 0.0 : Math.Round(Math.Max(0, Convert.ToDouble(reader["io_wait"])), 1);
                healthData["tempdb_usage"] = reader.IsDBNull("tempdb_usage") ? 0.0 : Math.Round(Math.Max(0, Math.Min(100, Convert.ToDouble(reader["tempdb_usage"]))), 1);
                healthData["active_connections"] = reader.IsDBNull("active_connections") ? 0 : Convert.ToInt32(reader["active_connections"]);
                healthData["longest_running_query"] = reader.IsDBNull("longest_running_query") ? 0 : Convert.ToInt32(reader["longest_running_query"]);
            }
            else
            {
                // Fallback values
                healthData["cpu_usage"] = 0.0;
                healthData["memory_usage"] = 0.0;
                healthData["io_wait"] = 0.0;
                healthData["tempdb_usage"] = 0.0;
                healthData["active_connections"] = 0;
                healthData["longest_running_query"] = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading SQL health from DMVs: {ErrorMessage}", ex.Message);
            // Return default values on error
            healthData["cpu_usage"] = 0.0;
            healthData["memory_usage"] = 0.0;
            healthData["io_wait"] = 0.0;
            healthData["tempdb_usage"] = 0.0;
            healthData["active_connections"] = 0;
            healthData["longest_running_query"] = 0;
        }

        return healthData;
    }

    public async Task<List<BatchJobHistory>> GetBatchJobHistoryAsync(string? captionPattern = null, DateTime? createdFrom = null)
    {
        var history = new List<BatchJobHistory>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    CAPTION,
                    STARTDATETIME,
                    ENDDATETIME,
                    REASON,
                    CREATEDDATETIME
                FROM BrasBatchJobHistoryTable
                WHERE 1=1";

            if (!string.IsNullOrEmpty(captionPattern))
            {
                // Support multiple patterns separated by comma or semicolon
                var patterns = captionPattern.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (patterns.Length > 0)
                {
                    var patternConditions = new List<string>();
                    foreach (var pattern in patterns)
                    {
                        var trimmedPattern = pattern.Trim();
                        if (trimmedPattern.Contains('%'))
                        {
                            patternConditions.Add($"CAPTION LIKE '{trimmedPattern.Replace("'", "''")}'");
                        }
                        else
                        {
                            patternConditions.Add($"CAPTION LIKE '{trimmedPattern.Replace("'", "''")}%'");
                        }
                    }
                    query += $" AND ({string.Join(" OR ", patternConditions)})";
                }
            }
            else
            {
                // Default: Show all history entries (remove filter)
                // query += " AND (CAPTION LIKE 'Auftragsfreige Distribution 0%' OR CAPTION LIKE 'Auftragsfreige Distribution 1%')";
            }

            if (createdFrom.HasValue)
            {
                query += $" AND CREATEDDATETIME >= '{createdFrom.Value:yyyy-MM-dd}'";
            }
            else
            {
                // Default: Last 30 days
                query += " AND CREATEDDATETIME >= DATEADD(DAY, -30, GETDATE())";
            }

            query += " ORDER BY STARTDATETIME DESC";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 60;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new BatchJobHistory
                {
                    Caption = reader["CAPTION"]?.ToString() ?? "",
                    StartDateTime = reader.IsDBNull("STARTDATETIME") ? null : reader.GetDateTime("STARTDATETIME"),
                    EndDateTime = reader.IsDBNull("ENDDATETIME") ? null : reader.GetDateTime("ENDDATETIME"),
                    Reason = reader["REASON"]?.ToString(),
                    CreatedDateTime = reader.IsDBNull("CREATEDDATETIME") ? null : reader.GetDateTime("CREATEDDATETIME")
                });
            }
            
            _logger.LogInformation("Retrieved {Count} batch job history records from AX database", history.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading batch job history from AX database: {ErrorMessage}. Table 'BrasBatchJobHistoryTable' might not exist.", ex.Message);
            // Return empty list instead of throwing to allow page to load
            return new List<BatchJobHistory>();
        }

        return history;
    }

    public async Task<BatchJobHistoryPageResult> GetBatchJobHistoryPageAsync(int page, int pageSize, string? captionPattern = null, DateTime? createdFrom = null)
    {
        var result = new BatchJobHistoryPageResult();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Validate page parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200); // Max 200 per page
            var skip = (page - 1) * pageSize;

            // Build WHERE clause
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrEmpty(captionPattern))
            {
                var patterns = captionPattern.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (patterns.Length > 0)
                {
                    var patternConditions = new List<string>();
                    foreach (var pattern in patterns)
                    {
                        var trimmedPattern = pattern.Trim();
                        if (trimmedPattern.Contains('%'))
                        {
                            patternConditions.Add($"CAPTION LIKE '{trimmedPattern.Replace("'", "''")}'");
                        }
                        else
                        {
                            patternConditions.Add($"CAPTION LIKE '{trimmedPattern.Replace("'", "''")}%'");
                        }
                    }
                    whereClause += $" AND ({string.Join(" OR ", patternConditions)})";
                }
            }

            if (createdFrom.HasValue)
            {
                whereClause += $" AND CREATEDDATETIME >= '{createdFrom.Value:yyyy-MM-dd}'";
            }
            else
            {
                whereClause += " AND CREATEDDATETIME >= DATEADD(DAY, -30, GETDATE())";
            }

            // Get total count
            var countQuery = $@"
                SELECT COUNT(*) 
                FROM BrasBatchJobHistoryTable
                {whereClause}";

            using var countCommand = new SqlCommand(countQuery, connection);
            countCommand.CommandTimeout = 60;
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

            // Get paged results
            var query = $@"
                SELECT 
                    CAPTION,
                    STARTDATETIME,
                    ENDDATETIME,
                    REASON,
                    CREATEDDATETIME
                FROM BrasBatchJobHistoryTable
                {whereClause}
                ORDER BY STARTDATETIME DESC
                OFFSET {skip} ROWS
                FETCH NEXT {pageSize} ROWS ONLY";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 60;

            var history = new List<BatchJobHistory>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new BatchJobHistory
                {
                    Caption = reader["CAPTION"]?.ToString() ?? "",
                    StartDateTime = reader.IsDBNull("STARTDATETIME") ? null : reader.GetDateTime("STARTDATETIME"),
                    EndDateTime = reader.IsDBNull("ENDDATETIME") ? null : reader.GetDateTime("ENDDATETIME"),
                    Reason = reader["REASON"]?.ToString(),
                    CreatedDateTime = reader.IsDBNull("CREATEDDATETIME") ? null : reader.GetDateTime("CREATEDDATETIME")
                });
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            result.Items = history;
            result.TotalCount = totalCount;
            result.Page = page;
            result.PageSize = pageSize;
            result.TotalPages = totalPages;
            
            _logger.LogInformation("Retrieved page {Page} of batch job history: {Count} items", page, history.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading batch job history page from AX database: {ErrorMessage}", ex.Message);
            result.Items = new List<BatchJobHistory>();
            result.TotalCount = 0;
        }

        return result;
    }

    public async Task<bool> RestartBatchJobInAXAsync(string batchJobRecId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // In AX, to restart a batch job, we need to:
            // 1. Set STATUS back to Waiting (1)
            // 2. Clear ENDDATETIME if present
            // 3. Optionally clear ERROR fields
            var query = @"
                UPDATE BATCHJOB 
                SET STATUS = 1,  -- Waiting
                    ENDDATETIME = NULL
                WHERE RECID = @RecId 
                AND STATUS IN (4, 5, 6)"; // Only restart Error, Cancelled, or Completed jobs

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@RecId", batchJobRecId);
            command.CommandTimeout = 30;

            var rowsAffected = await command.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Successfully restarted batch job {BatchJobRecId}", batchJobRecId);
                return true;
            }
            else
            {
                _logger.LogWarning("No batch job found or job cannot be restarted: {BatchJobRecId}", batchJobRecId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting batch job {BatchJobRecId}: {ErrorMessage}", batchJobRecId, ex.Message);
            return false;
        }
    }

    public async Task<bool> KillSessionInAXAsync(string sessionId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get the SQL Server SPID from sys.dm_exec_sessions using the AX session ID
            // Note: AX session ID needs to be mapped to SQL Server SPID
            // This is a simplified approach - in production, you might need to join with AX tables
            var getSpidQuery = @"
                SELECT TOP 1 s.session_id AS spid
                FROM sys.dm_exec_sessions s
                WHERE s.session_id > 50  -- User sessions only
                AND s.program_name LIKE '%AX%'
                ORDER BY s.login_time DESC";

            // Alternative: Try to get SPID from AX session context if available
            // For now, we'll use a more direct approach - find active sessions matching the pattern
            string? sqlServerSpid = null;
            
            // First, try to find the session in SYSCLIENTSESSIONS to get additional context
            var axSessionQuery = @"
                SELECT TOP 1 SESSIONID, USERID, SERVERID
                FROM SYSCLIENTSESSIONS 
                WHERE SESSIONID = @SessionId AND STATUS = 1"; // Active only

            string? userId = null;
            using (var command = new SqlCommand(axSessionQuery, connection))
            {
                command.Parameters.AddWithValue("@SessionId", sessionId);
                command.CommandTimeout = 30;
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    userId = reader["USERID"]?.ToString();
                }
            }

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Session {SessionId} not found in AX SYSCLIENTSESSIONS or not active", sessionId);
                return false;
            }

            // Try to find SQL Server SPID by matching user and program
            var findSpidQuery = @"
                SELECT TOP 1 s.session_id AS spid
                FROM sys.dm_exec_sessions s
                WHERE s.session_id > 50
                AND s.login_name = @UserId
                AND s.program_name LIKE '%Dynamics%'
                ORDER BY s.login_time DESC";

            using (var command = new SqlCommand(findSpidQuery, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.CommandTimeout = 30;
                
                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    sqlServerSpid = result.ToString();
                }
            }

            if (string.IsNullOrEmpty(sqlServerSpid))
            {
                _logger.LogWarning("Could not find SQL Server SPID for AX session {SessionId} (User: {UserId})", sessionId, userId);
                return false;
            }

            // Kill the SQL Server session using KILL command
            // WARNING: This requires ALTER ANY CONNECTION permission
            var killQuery = $"KILL {sqlServerSpid}";
            
            using var killCommand = new SqlCommand(killQuery, connection);
            killCommand.CommandTimeout = 30;
            
            await killCommand.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Successfully killed session {SessionId} (SPID: {Spid}, User: {UserId})", sessionId, sqlServerSpid, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing session {SessionId}: {ErrorMessage}", sessionId, ex.Message);
            return false;
        }
    }
}

public class BatchJobHistoryPageResult
{
    public List<BatchJobHistory> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

