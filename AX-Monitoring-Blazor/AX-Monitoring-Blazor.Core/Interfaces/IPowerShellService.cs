using AX_Monitoring_Blazor.Core.Interfaces;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IPowerShellService
    {
        Task<string> ExecuteScriptAsync(string script);
        Task<string> ExecuteScriptFileAsync(string scriptPath, Dictionary<string, object>? parameters = null);
        Task<bool> TestPowerShellAvailabilityAsync();
    }
}