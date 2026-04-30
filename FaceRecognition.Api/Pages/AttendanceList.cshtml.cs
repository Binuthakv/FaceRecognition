using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaceRecognitionApp.Api.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin,Manager")]
public class AttendanceListModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly ILogger<AttendanceListModel> _logger;

    private const int PAGE_SIZE = 10;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<AttendanceRecordViewModel> AttendanceRecords { get; set; } = new();
    public List<AttendanceRecordViewModel> PaginatedRecords { get; set; } = new();
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public AttendanceListModel(
        IAttendanceService attendanceService,
        IUserDatabaseService userDatabaseService,
        ILogger<AttendanceListModel> logger)
    {
        _attendanceService = attendanceService;
        _userDatabaseService = userDatabaseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            var attendanceRecords = await _attendanceService.GetAllAttendanceAsync();
            var users = await _userDatabaseService.GetAllUsersAsync();

            var userDictionary = users.ToDictionary(u => u.UserId, u => u.Name);

            AttendanceRecords = attendanceRecords
                .OrderByDescending(a => a.ScanTime)
                .Select(a => new AttendanceRecordViewModel
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Name = userDictionary.ContainsKey(a.UserId) ? userDictionary[a.UserId] : "Unknown",
                    ScanTime = a.ScanTime,
                    ScanTimeFormatted = a.ScanTime.ToString("g"),
                    Processed = a.Processed,
                    ProcessedStatus = a.Processed ? "Yes" : "No"
                })
                .ToList();

            // Calculate pagination
            TotalRecords = AttendanceRecords.Count;
            TotalPages = (int)Math.Ceiling((double)TotalRecords / PAGE_SIZE);

            // Validate page number
            if (PageNumber < 1)
                PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0)
                PageNumber = TotalPages;

            // Get paginated records
            PaginatedRecords = AttendanceRecords
                .Skip((PageNumber - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToList();

            StatusMessage = $"Successfully loaded {TotalRecords} attendance records.";
            _logger.LogInformation("Attendance records retrieved: {Count}", TotalRecords);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading attendance records: {ex.Message}";
            _logger.LogError(ex, "Error retrieving attendance records");
        }
    }
    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("User {User} logging out", User.Identity?.Name);
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToPage("/Login");
    }
}

public class AttendanceRecordViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime ScanTime { get; set; }
    public string ScanTimeFormatted { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public string ProcessedStatus { get; set; } = string.Empty;
}
