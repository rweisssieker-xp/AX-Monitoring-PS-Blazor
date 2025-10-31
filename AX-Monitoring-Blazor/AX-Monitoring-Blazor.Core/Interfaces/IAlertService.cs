using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IAlertService
    {
        Task<List<AlertDto>> GetAlertsAsync();
        Task<bool> AcknowledgeAlertAsync(long alertId);
        Task<bool> CheckAlertsAsync();
    }
}