using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration) => _configuration = configuration;

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Fixed reference-system accounts, for controlled testing only.
        var username = request.Username ?? string.Empty;
        var expectedPassword = username switch
        {
            "admin" => "password123",
            "test" => "passwordtest",
            _ => string.Empty
        };
        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(request.Password ?? string.Empty));
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(expectedPassword));
        if (expectedPassword.Length == 0 || !CryptographicOperations.FixedTimeEquals(candidate, expected))
            return Unauthorized(new { message = "Invalid Credentials" });

        var role = username == "admin" ? "Admin" : "User";
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var token = new JwtSecurityToken(
            issuer: "IgAPI",
            audience: "IgAPI.Client",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256));
        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), tokenType = "Bearer", expiresAt, role });
    }

    [Authorize(Roles = "Admin,User")]
    [HttpGet("session")]
    public IActionResult GetSession() => Ok(new
    {
        authenticated = true,
        username = User.FindFirstValue(ClaimTypes.NameIdentifier),
        role = User.FindFirstValue(ClaimTypes.Role)
    });
}

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
