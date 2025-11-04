using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        try
        {
            // TODO: Implement proper authentication (check against database/AD)
            // For now, simple hardcoded check
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
            {
                return BadRequest(new { error = "Username and password required" });
            }

            // Simple authentication (in production, use proper authentication)
            if (dto.Username == "admin" && dto.Password == "admin")
            {
                var jwtSecret = _configuration["Security:JwtSecret"] ?? "your-secret-key-change-in-production";
                var key = Encoding.UTF8.GetBytes(jwtSecret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("user_id", dto.Username),
                        new Claim("username", dto.Username),
                        new Claim("role", "admin")
                    }),
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
                        username = dto.Username,
                        role = "admin"
                    }
                });
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

    [HttpPost("refresh")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult RefreshToken()
    {
        // TODO: Implement token refresh logic
        return Ok(new { message = "Token refresh not implemented" });
    }
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

