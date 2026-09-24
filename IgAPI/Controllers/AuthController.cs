using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;



[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    //request from User side 

    // method on user login information 

    // POST request to authenticate user and generate JWT token

    // create a session for the user and return the token

    // POST api/auth/login    => JWT 

    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Validate user credentials (this is just a placeholder, implement your own logic)

        var expectedUsername = _configuration["DemoUser:Username"];
        var expectedPassword = _configuration["DemoUser:Password"];

        // Trim whitespace and compare
        var requestUsername = request.Username?.Trim() ?? "";
        var requestPassword = request.Password?.Trim() ?? "";
        var configUsername = expectedUsername?.Trim() ?? "";
        var configPassword = expectedPassword?.Trim() ?? "";

        if (string.IsNullOrEmpty(configUsername) || string.IsNullOrEmpty(configPassword))
        {
            return BadRequest(new { message = "Server configuration error" });
        }

        if (requestUsername != configUsername || requestPassword != configPassword || string.IsNullOrEmpty(requestUsername))
        {
            return Unauthorized(new { message = "Invalid Credentials" }); 
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var jwtKey = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        // token issuer and audience
        var token = new JwtSecurityToken(

            issuer: "IgAPI",
            audience: "IgAPI.Client",

            claims:

            [
                new Claim(ClaimTypes.NameIdentifier, request.Username),
                new Claim("jti", Guid.NewGuid().ToString())

            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)



            );

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer",
            expiresAt
        });

    }
    // session endpoint to check if the user is authenticated and return the username

    [Authorize]
    [HttpGet("session")]
    public IActionResult GetSession()
    {
        return Ok(new
        {
            authenticated = true,
            username = User.FindFirstValue(ClaimTypes.NameIdentifier)

        });
    
    }

}

public class LoginRequest
{
    public  required string Username { get; set; }
    public  required string Password { get; set; }
}