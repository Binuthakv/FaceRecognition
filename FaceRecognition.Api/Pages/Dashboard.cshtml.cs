using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using FaceRecognitionApp.Api.Services;

namespace FaceRecognitionApp.Api.Pages;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class DashboardModel : PageModel
{
    private readonly IUserDatabaseService _databaseService;
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<DashboardModel> _logger;

    public int TotalUsers { get; set; }
    public int TodayAttendance { get; set; }
    public int ThisWeekAttendance { get; set; }
    public DateTime? LastLoginDate { get; set; }

    public DashboardModel(
        IUserDatabaseService databaseService,
        IAttendanceService attendanceService,
        ILogger<DashboardModel> logger)
    {
        _databaseService = databaseService;
        _attendanceService = attendanceService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Loading dashboard for user: {User}", User.Identity?.Name);

            // Get total users count
            TotalUsers = await _databaseService.GetUserCountAsync();

            // Get today's attendance count
            var allAttendance = await _attendanceService.GetAllAttendanceAsync();
            TodayAttendance = allAttendance
                .Where(a => a.ScanTime.Date == DateTime.Today)
                .DistinctBy(a => a.UserId)
                .Count();

            // Get this week's attendance count
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            ThisWeekAttendance = allAttendance
                .Where(a => a.ScanTime.Date >= weekStart && a.ScanTime.Date <= DateTime.Today)
                .Count();

            _logger.LogInformation("Dashboard loaded - Total Users: {TotalUsers}, Today: {Today}, This Week: {Week}",
                TotalUsers, TodayAttendance, ThisWeekAttendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("User {User} logging out", User.Identity?.Name);
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToPage("/Login");
    }
}
