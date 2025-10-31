using AX_Monitoring_Blazor.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class ConfigService : IConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly string _environment;

        public ConfigService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _environment = webHostEnvironment.EnvironmentName;
        }

        public T GetConfigValue<T>(string key)
        {
            return _configuration.GetValue<T>(key);
        }

        public async Task<bool> ValidateConfigAsync()
        {
            // Check for required configuration values
            var axDbServer = _configuration["ConnectionStrings:AXDatabase"];
            var stagingDbServer = _configuration["ConnectionStrings:StagingDatabase"];

            if (string.IsNullOrEmpty(axDbServer) || string.IsNullOrEmpty(stagingDbServer))
            {
                return false;
            }

            // Additional validation can be added here
            await Task.CompletedTask;
            return true;
        }

        public string GetEnvironment()
        {
            return _environment;
        }
    }
}