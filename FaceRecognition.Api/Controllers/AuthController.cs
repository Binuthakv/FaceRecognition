using Microsoft.AspNetCore.Mvc;
using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using FaceRecognitionApp.Api.Helpers;

namespace FaceRecognitionApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserDatabaseService _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserDatabaseService db, ILogger<AuthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        try
        {
            _logger.LogInformation("Login attempt for username/email: {UsernameOrEmail}", request.Username);

            // Try to find user by username or email
            AdminUser? adminUser = await _db.GetAdminUserByUsernameAsync(request.Username);
            adminUser ??= await _db.GetAdminUserByEmailAsync(request.Username);

            if (adminUser == null)
            {
                _logger.LogWarning("Login failed: User not found - {UsernameOrEmail}", request.Username);
                return Unauthorized(new { message = "Invalid username/email or password." });
            }

            if (!adminUser.IsActive)
            {
                _logger.LogWarning("Login failed: User account is inactive - {Username}", adminUser.Username);
                return Unauthorized(new { message = "User account is inactive." });
            }

            // Verify password using BCrypt
            if (!PasswordHasher.VerifyPassword(request.Password, adminUser.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user - {Username}", adminUser.Username);
                return Unauthorized(new { message = "Invalid username/email or password." });
            }

            // Update last login date
            await _db.UpdateAdminUserLastLoginAsync(adminUser.Id);

            _logger.LogInformation("User '{Username}' logged in successfully", adminUser.Username);

            return Ok(new AdminLoginResponse(
                adminUser.Id,
                adminUser.Username,
                adminUser.Email,
                adminUser.Role,
                adminUser.IsActive));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during login." });
        }
    }
}

/// <summary>
/// Request model for admin login
/// </summary>
public record AdminLoginRequest(string Username, string Password, bool RememberMe);

/// <summary>
/// Response model for admin login
/// </summary>
public record AdminLoginResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    bool IsActive);
