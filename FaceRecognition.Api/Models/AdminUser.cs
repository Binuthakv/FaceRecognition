namespace FaceRecognitionApp.Api.Models;

/// <summary>
/// Represents an administrator user account.
/// </summary>
public class AdminUser
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Admin"; // Admin, Manager, Viewer

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginDate { get; set; }
}
