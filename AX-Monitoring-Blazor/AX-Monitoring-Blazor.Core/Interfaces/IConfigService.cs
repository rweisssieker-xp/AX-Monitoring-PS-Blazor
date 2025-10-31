namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IConfigService
    {
        T GetConfigValue<T>(string key);
        Task<bool> ValidateConfigAsync();
        string GetEnvironment();
    }
}