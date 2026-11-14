using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Models;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly List<User> _users = new();
    private static int _nextId = 1;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public IActionResult Register([FromBody] AuthRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Username and password are required." });

        if (_users.Any(u => u.Username.Equals(req.Username, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "Username already exists." });

        var user = new User
        {
            Id = _nextId++,
            Username = req.Username.Trim(),
            // Step 5: Seed admin role when username is "admin"
            Role = (req.Username.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)) ? "Admin" : "User"
        };

        user.PasswordHash = _hasher.HashPassword(user, req.Password);
        _users.Add(user);

        return StatusCode(201, new { user.Id, user.Username, user.Role });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] AuthRequest req)
    {
        var user = _users.FirstOrDefault(u =>
            u.Username.Equals(req.Username?.Trim(), StringComparison.OrdinalIgnoreCase));

        // Vague login error for security hygiene
        if (user is null)
            return Unauthorized(new { message = "Invalid username or password." });

        var verifyResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "");
        if (verifyResult == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid username or password." });

        var token = GenerateJwtToken(user);
        return Ok(new { token, username = user.Username, role = user.Role });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role) // Matches [Authorize(Roles = "Admin")]
        };

        var expiryMinutes = double.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}