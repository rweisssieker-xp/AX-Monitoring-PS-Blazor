using AX_Monitoring_Blazor.Shared;

namespace AX_Monitoring_Blazor.Core.Interfaces
{
    public interface IAuthService
    {
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task<UserDto> GetUserAsync(string username);
        Task<string> CreateJwtTokenAsync(UserDto user);
        Task<Dictionary<string, object>?> ValidateJwtTokenAsync(string token);
        Task<bool> HasPermissionAsync(string userRole, string requiredRole);
        Task<List<Dictionary<string, string>>> GetAvailableRolesAsync();
    }
}