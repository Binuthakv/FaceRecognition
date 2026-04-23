using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using FaceRecognitionApp.Api.Helpers;

namespace FaceRecognitionApp.Api.Pages;

public class LoginModel : PageModel
{
    private readonly IUserDatabaseService _databaseService;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public LoginModel(IUserDatabaseService databaseService, ILogger<LoginModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public void OnGet()
    {
        // Check if user is already logged in
        if (User.Identity?.IsAuthenticated ?? false)
        {
            Redirect("/Dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            return Page();
        }

        try
        {
            _logger.LogInformation("Login attempt for username/email: {UsernameOrEmail}", Username);

            // Try to find user by username or email
            AdminUser? adminUser = await _databaseService.GetAdminUserByUsernameAsync(Username);
            adminUser ??= await _databaseService.GetAdminUserByEmailAsync(Username);

            if (adminUser == null)
            {
                _logger.LogWarning("Login failed: User not found - {UsernameOrEmail}", Username);
                ErrorMessage = "Invalid username/email or password.";
                return Page();
            }

            // Verify password using BCrypt
            if (!PasswordHasher.VerifyPassword(Password, adminUser.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user - {Username}", adminUser.Username);
                ErrorMessage = "Invalid username/email or password.";
                return Page();
            }

            // Update last login date
            await _databaseService.UpdateAdminUserLastLoginAsync(adminUser.Id);

            // Create session/claims
            var claims = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, adminUser.Username),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, adminUser.Email),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, adminUser.Role),
                }, "AdminAuth"));

            var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = RememberMe,
                ExpiresUtc = RememberMe 
                    ? System.DateTimeOffset.UtcNow.AddDays(30) 
                    : System.DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync("AdminCookie", claims, props);

            _logger.LogInformation("User '{Username}' logged in successfully", adminUser.Username);
            SuccessMessage = $"Welcome back, {adminUser.Username}!";

            return RedirectToPage("/Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            ErrorMessage = "An error occurred during login. Please try again.";
            return Page();
        }
    }
}
