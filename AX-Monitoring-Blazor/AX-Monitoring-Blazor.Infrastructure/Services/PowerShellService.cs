using AX_Monitoring_Blazor.Core.Interfaces;
using System.Diagnostics;
using System.Text;

namespace AX_Monitoring_Blazor.Infrastructure.Services
{
    public class PowerShellService : IPowerShellService
    {
        private readonly ILogger<PowerShellService> _logger;

        public PowerShellService(ILogger<PowerShellService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            _logger.LogInformation("Executing PowerShell script");
            
            return await ExecutePowerShellCommand($"-Command \"{script}\"");
        }

        public async Task<string> ExecuteScriptFileAsync(string scriptPath, Dictionary<string, object>? parameters = null)
        {
            _logger.LogInformation($"Executing PowerShell script file: {scriptPath}");
            
            var command = $"-File \"{scriptPath}\"";
            
            if (parameters != null && parameters.Any())
            {
                var paramStr = string.Join(" ", parameters.Select(p => $"-{p.Key} {p.Value}"));
                command += $" {paramStr}";
            }
            
            return await ExecutePowerShellCommand(command);
        }

        public async Task<bool> TestPowerShellAvailabilityAsync()
        {
            try
            {
                var result = await ExecutePowerShellCommand("-Command \"Get-Host\"");
                return !string.IsNullOrEmpty(result);
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ExecutePowerShellCommand(string command)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _logger.LogError($"PowerShell execution failed with exit code {process.ExitCode}: {error}");
                    throw new InvalidOperationException($"PowerShell execution failed: {error}");
                }
                
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing PowerShell command: {Command}", command);
                throw;
            }
        }
    }
}