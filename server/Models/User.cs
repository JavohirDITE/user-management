using System.ComponentModel.DataAnnotations;

namespace UserManagement.API.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public string Status { get; set; } = "unverified";
    
    public string? VerificationToken { get; set; }
    
    public DateTime? LastLogin { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
