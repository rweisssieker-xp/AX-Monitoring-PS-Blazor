using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace AXMonitoringBU.Api.Services;

public interface IUserService
{
    string GetCurrentWindowsUser();
    string? GetCurrentUser();
}

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentWindowsUser()
    {
        try
        {
            // Try to get Windows identity from HTTP context
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var identity = httpContext.User.Identity;
                if (identity is WindowsIdentity windowsIdentity)
                {
                    return windowsIdentity.Name;
                }
                
                // Try to get from claims
                var windowsClaim = httpContext.User.FindFirst(ClaimTypes.WindowsAccountName)?.Value
                    ?? httpContext.User.FindFirst("windows_account_name")?.Value;
                
                if (!string.IsNullOrEmpty(windowsClaim))
                {
                    return windowsClaim;
                }
                
                // Fallback to identity name
                if (!string.IsNullOrEmpty(identity.Name))
                {
                    return identity.Name;
                }
            }

            // Fallback: Get current Windows user
            var currentWindowsIdentity = WindowsIdentity.GetCurrent();
            if (currentWindowsIdentity != null)
            {
                return currentWindowsIdentity.Name;
            }
        }
        catch
        {
            // If Windows identity is not available, return default
        }

        // Last fallback: Environment username
        return Environment.UserName ?? "SYSTEM";
    }

    public string? GetCurrentUser()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var username = httpContext.User.FindFirst("username")?.Value
                    ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? httpContext.User.Identity?.Name;
                
                if (!string.IsNullOrEmpty(username))
                {
                    return username;
                }
            }

            return GetCurrentWindowsUser();
        }
        catch
        {
            return GetCurrentWindowsUser();
        }
    }
}

