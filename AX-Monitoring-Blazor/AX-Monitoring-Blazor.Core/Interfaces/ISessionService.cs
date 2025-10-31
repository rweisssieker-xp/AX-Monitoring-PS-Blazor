using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface ISessionService
    {
        Task<List<SessionDto>> GetSessionsAsync(bool activeOnly = false);
        Task<Dictionary<string, object>> GetSessionStatisticsAsync();
    }
}