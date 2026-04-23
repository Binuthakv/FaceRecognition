using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using FaceRecognitionApp.Api.Helpers;

namespace FaceRecognitionApp.Api.Pages;

public class SetupModel : PageModel
{
    private readonly IUserDatabaseService _databaseService;
    private readonly ILogger<SetupModel> _logger;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = "Admin";

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool AdminExists { get; set; }

    public SetupModel(IUserDatabaseService databaseService, ILogger<SetupModel> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Check if admin already exists
            var admin = await _databaseService.GetAdminUserByUsernameAsync("admin");
            AdminExists = admin != null;

            if (AdminExists)
            {
                _logger.LogInformation("Admin setup accessed but admin already exists");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin existence");
            ErrorMessage = "An error occurred while checking admin status.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || 
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            if (Username.Length < 3)
            {
                ErrorMessage = "Username must be at least 3 characters.";
                return Page();
            }

            if (!Email.Contains("@"))
            {
                ErrorMessage = "Please enter a valid email address.";
                return Page();
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            // Check if admin already exists
            var existingAdmin = await _databaseService.GetAdminUserByUsernameAsync(Username);
            if (existingAdmin != null)
            {
                ErrorMessage = "Username already exists. Please choose a different username.";
                return Page();
            }

            var existingEmail = await _databaseService.GetAdminUserByEmailAsync(Email);
            if (existingEmail != null)
            {
                ErrorMessage = "Email already exists. Please use a different email.";
                return Page();
            }

            // Create the admin user
            var adminUser = new AdminUser
            {
                Username = Username.Trim(),
                Email = Email.Trim(),
                PasswordHash = PasswordHasher.HashPassword(Password),
                Role = Role,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _databaseService.SaveAdminUserAsync(adminUser);

            _logger.LogInformation("Admin user '{Username}' created successfully", adminUser.Username);
            SuccessMessage = $"Admin account '{Username}' created successfully!";
            AdminExists = true;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin setup");
            ErrorMessage = "An error occurred during setup. Please try again.";
            return Page();
        }
    }
}
