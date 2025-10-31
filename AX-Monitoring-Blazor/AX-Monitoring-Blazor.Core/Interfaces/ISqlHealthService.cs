using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface ISqlHealthService
    {
        Task<SqlHealthDto> GetSqlHealthMetricsAsync();
        Task<List<Dictionary<string, object>>> GetDatabaseSizeAsync();
    }
}