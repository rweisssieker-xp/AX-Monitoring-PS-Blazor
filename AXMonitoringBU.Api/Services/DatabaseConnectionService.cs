using Microsoft.Data.SqlClient;
using System.Data;

namespace AXMonitoringBU.Api.Services;

public interface IDatabaseConnectionService
{
    Task<SqlConnection> GetConnectionAsync();
    Task<T> ExecuteWithRetryAsync<T>(Func<SqlConnection, Task<T>> operation, int maxRetries = 3);
    void ConfigureConnectionPooling(SqlConnectionStringBuilder builder);
}

public class DatabaseConnectionService : IDatabaseConnectionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseConnectionService> _logger;
    private readonly string _connectionString;

    public DatabaseConnectionService(
        IConfiguration configuration,
        ILogger<DatabaseConnectionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _configuration["Database:Server"] ?? "localhost",
                InitialCatalog = _configuration["Database:Name"] ?? "AX2012R3",
                IntegratedSecurity = _configuration["Database:UseWindowsAuthentication"] == "true" || 
                                    string.IsNullOrEmpty(_configuration["Database:User"]),
                TrustServerCertificate = true
            };

            if (!builder.IntegratedSecurity)
            {
                builder.UserID = _configuration["Database:User"] ?? "";
                builder.Password = _configuration["Database:Password"] ?? "";
            }

            // Connection Pooling Configuration
            ConfigureConnectionPooling(builder);

            // Timeout Configuration
            builder.ConnectTimeout = int.Parse(_configuration["Database:ConnectionTimeout"] ?? "30");
            builder.CommandTimeout = int.Parse(_configuration["Database:CommandTimeout"] ?? "60");

            _connectionString = builder.ConnectionString;
        }
        else
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            ConfigureConnectionPooling(builder);
            _connectionString = builder.ConnectionString;
        }
    }

    public void ConfigureConnectionPooling(SqlConnectionStringBuilder builder)
    {
        // Connection Pooling Settings
        builder.Pooling = true;
        builder.MinPoolSize = int.Parse(_configuration["Database:MinPoolSize"] ?? "5");
        builder.MaxPoolSize = int.Parse(_configuration["Database:MaxPoolSize"] ?? "100");
        builder.ConnectRetryCount = int.Parse(_configuration["Database:ConnectRetryCount"] ?? "3");
        builder.ConnectRetryInterval = int.Parse(_configuration["Database:ConnectRetryInterval"] ?? "10");
        
        // Connection Lifetime (0 = unlimited, connections are not recycled)
        builder.LoadBalanceTimeout = int.Parse(_configuration["Database:LoadBalanceTimeout"] ?? "0");
    }

    public async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<SqlConnection, Task<T>> operation, int maxRetries = 3)
    {
        var retryCount = 0;
        Exception? lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                using var connection = await GetConnectionAsync();
                return await operation(connection);
            }
            catch (SqlException ex) when (IsTransientError(ex) && retryCount < maxRetries - 1)
            {
                lastException = ex;
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff
                _logger.LogWarning(ex, "Transient error occurred, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})", 
                    delay.TotalSeconds, retryCount, maxRetries);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-transient error occurred");
                throw;
            }
        }

        _logger.LogError(lastException, "Operation failed after {MaxRetries} retries", maxRetries);
        throw lastException ?? new InvalidOperationException("Operation failed after retries");
    }

    private bool IsTransientError(SqlException ex)
    {
        // SQL Server transient error numbers
        var transientErrors = new[]
        {
            2,      // Timeout expired
            53,     // Network-related error
            121,    // Semaphore timeout
            1205,   // Deadlock victim
            1222,   // Lock request timeout
            8645,   // A timeout occurred while waiting for memory
            8651,   // Low memory condition
            4060,   // Cannot open database
            40197,  // Service has encountered an error
            40501,  // Service is currently busy
            40613,  // Database on server is not currently available
            49918,  // Cannot process request
            49919,  // Cannot process create or update request
            49920   // Cannot process request
        };

        return transientErrors.Contains(ex.Number);
    }
}

