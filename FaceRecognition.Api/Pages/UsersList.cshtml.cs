using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaceRecognitionApp.Api.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class UsersListModel : PageModel
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly ILogger<UsersListModel> _logger;

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "name";

    public List<UserViewModel> Users { get; set; } = new();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalUsers { get; set; }
    public int UsersWithCompletePhotos { get; set; }

    public UsersListModel(
        IUserDatabaseService userDatabaseService,
        ILogger<UsersListModel> logger)
    {
        _userDatabaseService = userDatabaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading users list for admin: {User}", User.Identity?.Name);

            var allUsers = await _userDatabaseService.GetAllUsersAsync();
            TotalUsers = allUsers.Count;
            UsersWithCompletePhotos = allUsers.Count(u => u.HasAllPhotos);

            var viewModels = allUsers
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    Name = u.Name,
                    DateOfBirth = u.DateOfBirth,
                    Age = u.Age,
                    Sex = u.Sex,
                    RegisteredDate = u.RegisteredDate,
                    PhotoCount = (u.Photo1 != null ? 1 : 0) + (u.Photo2 != null ? 1 : 0) + (u.Photo3 != null ? 1 : 0),
                    HasAllPhotos = u.HasAllPhotos
                })
                .ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var searchLower = SearchQuery.ToLower();
                viewModels = viewModels
                    .Where(u => u.Name.ToLower().Contains(searchLower) ||
                               u.UserId.ToLower().Contains(searchLower))
                    .ToList();
            }

            // Apply sorting
            viewModels = SortBy switch
            {
                "userid" => viewModels.OrderBy(u => u.UserId).ToList(),
                "age" => viewModels.OrderByDescending(u => u.Age).ToList(),
                "registered" => viewModels.OrderByDescending(u => u.RegisteredDate).ToList(),
                _ => viewModels.OrderBy(u => u.Name).ToList()
            };

            Users = viewModels;

            if (Users.Count == 0 && !string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = $"No users found matching '{SearchQuery}'.";
            }
            else
            {
                StatusMessage = $"Showing {Users.Count} of {TotalUsers} users.";
            }

            _logger.LogInformation("Users list loaded - Total: {Total}, Displayed: {Count}", TotalUsers, Users.Count);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading users: {ex.Message}";
            _logger.LogError(ex, "Error retrieving users list");
        }
    }

    public async Task<IActionResult> OnGetDeleteAsync(int userId)
    {
        try
        {
            var user = await _userDatabaseService.GetUserAsync(userId);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return RedirectToPage();
            }

            await _userDatabaseService.DeleteUserAsync(user);
            await _userDatabaseService.DeleteUserEmbeddingsAsync(user.UserId);

            StatusMessage = $"User '{user.Name}' (ID: {user.UserId}) deleted successfully.";
            _logger.LogInformation("User {UserId} deleted by {Admin}", user.UserId, User.Identity?.Name);

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting user: {ex.Message}";
            _logger.LogError(ex, "Error deleting user");
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("User {User} logging out", User.Identity?.Name);
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToPage("/Login");
    }
}

public class UserViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public int PhotoCount { get; set; }
    public bool HasAllPhotos { get; set; }
}
