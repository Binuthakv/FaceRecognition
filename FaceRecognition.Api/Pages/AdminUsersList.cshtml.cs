using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaceRecognitionApp.Api.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public class AdminUsersListModel : PageModel
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly ILogger<AdminUsersListModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "username";

    public List<AdminUserViewModel> AdminUsers { get; set; } = new();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalAdminUsers { get; set; }
    public int ActiveAdminUsers { get; set; }
    public int InactiveAdminUsers { get; set; }

    public AdminUsersListModel(
        IUserDatabaseService userDatabaseService,
        ILogger<AdminUsersListModel> logger)
    {
        _userDatabaseService = userDatabaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading admin users list for admin: {User}", User.Identity?.Name);

            var allAdminUsers = await _userDatabaseService.GetAllAdminUsersAsync();
            TotalAdminUsers = allAdminUsers.Count;
            ActiveAdminUsers = allAdminUsers.Count(u => u.IsActive);
            InactiveAdminUsers = allAdminUsers.Count(u => !u.IsActive);

            var viewModels = allAdminUsers
                .Select(u => new AdminUserViewModel
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                })
                .ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var searchLower = SearchQuery.ToLower();
                viewModels = viewModels
                    .Where(u => u.Username.ToLower().Contains(searchLower) ||
                               u.Email.ToLower().Contains(searchLower))
                    .ToList();
            }

            // Apply sorting
            viewModels = SortBy switch
            {
                "email" => viewModels.OrderBy(u => u.Email).ToList(),
                "role" => viewModels.OrderBy(u => u.Role).ToList(),
                "created" => viewModels.OrderByDescending(u => u.CreatedDate).ToList(),
                "lastlogin" => viewModels.OrderByDescending(u => u.LastLoginDate).ToList(),
                _ => viewModels.OrderBy(u => u.Username).ToList()
            };

            AdminUsers = viewModels;

            if (AdminUsers.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = $"No admin users found matching '{SearchQuery}'.";
            }
            else
            {
                StatusMessage = $"Showing {AdminUsers.Count} of {TotalAdminUsers} admin users.";
            }

            _logger.LogInformation("Admin users list loaded - Total: {Total}, Displayed: {Count}", TotalAdminUsers, AdminUsers.Count);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading admin users: {ex.Message}";
            _logger.LogError(ex, "Error retrieving admin users list");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("User {User} logging out", User.Identity?.Name);
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToPage("/Login");
    }
}

public class AdminUserViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
