using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.API.Data;
using UserManagement.API.DTOs;
using UserManagement.API.Models;
using UserManagement.API.Services;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context, 
        IConfiguration config, 
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
        _logger = logger;
    }

    // Helper function for generating unique identifiers
    private static string GetUniqIdValue(string prefix = "id")
    {
        return $"{prefix}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required" });
        
        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Password is required" });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required" });

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = "unverified",
            VerificationToken = GetUniqIdValue("verify"),
            CreatedAt = DateTime.UtcNow
        };

        // Here I don't check if email exists in code - 
        // instead I rely on the database unique index to enforce uniqueness.
        // If someone tries to register with existing email, PostgreSQL will throw
        // an exception with code 23505 which I catch below
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Database threw unique constraint violation - email already exists
            _logger.LogWarning("Duplicate email registration attempt: {Email}", dto.Email);
            return Conflict(new { message = "User with this email already exists" });
        }

        // Send verification email in background (fire and forget)
        _ = _emailService.SendVerificationEmailAsync(user.Email, user.VerificationToken!);

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            User = MapToDto(user)
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email and password are required" });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        // Blocked users can't login
        if (user.Status == "blocked")
            return Forbid();

        // Update last login time
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponseDto
        {
            Token = token,
            User = MapToDto(user)
        });
    }

    [HttpGet("verify/{token}")]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.VerificationToken == token);

        if (user == null)
            return NotFound(new { message = "Invalid verification token" });

        if (user.Status == "active")
            return Ok(new { message = "Email already verified" });

        // If user is blocked, keep them blocked
        if (user.Status == "blocked")
            return Ok(new { message = "User is blocked" });

        user.Status = "active";
        user.VerificationToken = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Email verified successfully" });
    }

    // Checks if the database exception is a unique constraint violation
    // PostgreSQL uses error code 23505 for this
    private bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pgEx)
        {
            return pgEx.SqlState == "23505"; // unique_violation
        }
        return false;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "SuperSecretKeyThatIsAtLeast32Characters!"));
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "UserManagement",
            audience: _config["Jwt:Audience"] ?? "UserManagement",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Status = user.Status,
        LastLogin = user.LastLogin,
        CreatedAt = user.CreatedAt
    };
}
