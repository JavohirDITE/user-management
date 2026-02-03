using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagement.API.Data;
using UserManagement.API.DTOs;
using UserManagement.API.Models;

namespace UserManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AppDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static string GetUniqIdValue(string prefix = "id")
    {
        return $"{prefix}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}";
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.LastLogin)
            .ThenByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Status = u.Status,
                LastLogin = u.LastLogin,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockUsers([FromBody] UserIdsDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return BadRequest(new { message = "No users selected" });

        var currentUserId = GetCurrentUserId();
        
        var users = await _context.Users
            .Where(u => dto.Ids.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = "blocked";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Blocked {Count} users by user {UserId}", users.Count, currentUserId);

        var selfBlocked = dto.Ids.Contains(currentUserId);

        return Ok(new { 
            message = $"Blocked {users.Count} user(s)", 
            count = users.Count,
            selfBlocked 
        });
    }

    [HttpPost("unblock")]
    public async Task<IActionResult> UnblockUsers([FromBody] UserIdsDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return BadRequest(new { message = "No users selected" });

        var users = await _context.Users
            .Where(u => dto.Ids.Contains(u.Id))
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = "active";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Unblocked {Count} users", users.Count);

        return Ok(new { message = $"Unblocked {users.Count} user(s)", count = users.Count });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteUsers([FromBody] UserIdsDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return BadRequest(new { message = "No users selected" });

        var currentUserId = GetCurrentUserId();

        var users = await _context.Users
            .Where(u => dto.Ids.Contains(u.Id))
            .ToListAsync();

        _context.Users.RemoveRange(users);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} users by user {UserId}", users.Count, currentUserId);

        var selfDeleted = dto.Ids.Contains(currentUserId);

        return Ok(new { 
            message = $"Deleted {users.Count} user(s)", 
            count = users.Count,
            selfDeleted 
        });
    }

    [HttpDelete("unverified")]
    public async Task<IActionResult> DeleteUnverifiedUsers()
    {
        var unverifiedUsers = await _context.Users
            .Where(u => u.Status == "unverified")
            .ToListAsync();

        var count = unverifiedUsers.Count;
        var currentUserId = GetCurrentUserId();
        var selfDeleted = unverifiedUsers.Any(u => u.Id == currentUserId);

        _context.Users.RemoveRange(unverifiedUsers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} unverified users", count);

        return Ok(new { 
            message = $"Deleted {count} unverified user(s)", 
            count,
            selfDeleted 
        });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Status = user.Status,
            LastLogin = user.LastLogin,
            CreatedAt = user.CreatedAt
        });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }
}
