using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserService _userService;

    public AuthController(
        IConfiguration configuration, 
        ILogger<AuthController> logger,
        IUserService userService)
    {
        _configuration = configuration;
        _logger = logger;
        _userService = userService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto? dto)
    {
        try
        {
            // Get Windows user if available
            var windowsUser = _userService.GetCurrentWindowsUser();
            
            // If no credentials provided, try to use Windows authentication
            if (dto == null || (string.IsNullOrEmpty(dto.Username) && string.IsNullOrEmpty(dto.Password)))
            {
                if (!string.IsNullOrEmpty(windowsUser) && windowsUser != "SYSTEM")
                {
                    // Use Windows user for authentication
                    return CreateTokenForUser(windowsUser, "user");
                }
                else
                {
                    return BadRequest(new { error = "Username and password required, or Windows authentication must be enabled" });
                }
            }

            // Validate credentials
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new { error = "Username and password required" });
            }

            // Simple authentication (in production, use proper authentication against AD or database)
            // For now, allow Windows user or admin/admin
            var isAdmin = (dto.Username == "admin" && dto.Password == "admin") ||
                         (dto.Username.Equals(windowsUser, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(windowsUser));
            
            if (isAdmin || dto.Username == "admin")
            {
                return CreateTokenForUser(dto.Username, "admin");
            }
            else
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "Login failed" });
        }
    }

    private IActionResult CreateTokenForUser(string username, string role)
    {
        var jwtSecret = _configuration["Security:JwtSecret"] ?? "your-secret-key-change-in-production";
        var key = Encoding.UTF8.GetBytes(jwtSecret);
        
        var windowsUser = _userService.GetCurrentWindowsUser();
        var claims = new List<Claim>
        {
            new Claim("user_id", username),
            new Claim("username", username),
            new Claim("role", role)
        };
        
        if (!string.IsNullOrEmpty(windowsUser) && windowsUser != username)
        {
            claims.Add(new Claim("windows_account_name", windowsUser));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new
        {
            access_token = tokenString,
            token_type = "bearer",
            expires_in = 86400, // 24 hours in seconds
            user = new
            {
                username = username,
                windows_user = windowsUser,
                role = role
            }
        });
    }

    [HttpGet("current-user")]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var windowsUser = _userService.GetCurrentWindowsUser();
            var currentUser = _userService.GetCurrentUser();
            
            return Ok(new
            {
                windows_user = windowsUser,
                current_user = currentUser,
                authenticated = User.Identity?.IsAuthenticated ?? false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { error = "Failed to get current user" });
        }
    }

    [HttpPost("refresh")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult RefreshToken()
    {
        try
        {
            // Get current user from token claims
            var username = User.FindFirst("username")?.Value ?? User.FindFirst("user_id")?.Value;
            var role = User.FindFirst("role")?.Value ?? "user";

            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            // Create new token with same user info
            return CreateTokenForUser(username, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { error = "Token refresh failed" });
        }
    }
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

