using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaceRecognitionApp.Api.Pages;

public class UserWorkingHoursListModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IUserWorkingHoursService _userWorkingHoursService;
    private readonly ILogger<UserWorkingHoursListModel> _logger;

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ViewType { get; set; } = "daily"; // daily, weekly, monthly

    public List<UserWorkingHoursViewModel> WorkingHoursRecords { get; set; } = new();
    public List<DailyWorkingHoursViewModel> DailyWorkingHours { get; set; } = new();
    public List<WeeklyWorkingHoursViewModel> WeeklyWorkingHours { get; set; } = new();
    public List<MonthlyWorkingHoursViewModel> MonthlyWorkingHours { get; set; } = new();
    public List<UserDropdownItem> Users { get; set; } = new();
    public List<string> UniqueUserIds { get; set; } = new();
    public Dictionary<string, string> UserIdToNameMap { get; set; } = new();
    public string? StatusMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // Summary statistics
    public int TotalRecords { get; set; }
    public decimal TotalHours { get; set; }
    public decimal AverageHours { get; set; }
    public int UniqueUsers { get; set; }

    // Processing summary
    public WorkingHoursProcessingSummary? ProcessingSummary { get; set; }
    public bool ShowProcessingInfo { get; set; }

    public UserWorkingHoursListModel(
        IAttendanceService attendanceService,
        IUserDatabaseService userDatabaseService,
        IUserWorkingHoursService userWorkingHoursService,
        ILogger<UserWorkingHoursListModel> logger)
    {
        _attendanceService = attendanceService;
        _userDatabaseService = userDatabaseService;
        _userWorkingHoursService = userWorkingHoursService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("UserWorkingHoursList page loading - Starting working hours processing");

            // Process working hours for today to get fresh calculations
            var processDate = DateTime.Today;
            _logger.LogInformation("Processing working hours for date: {Date}", processDate);

            ProcessingSummary = await _userWorkingHoursService.ProcessUserWorkingHoursAsync(processDate);

            if (ProcessingSummary?.TotalUsersProcessed > 0)
            {
                ShowProcessingInfo = true;
                _logger.LogInformation(
                    "Working hours processed - Processed: {Count}, Skipped: {Skipped}",
                    ProcessingSummary.TotalUsersProcessed,
                    ProcessingSummary.UsersSkipped);
            }
            else if (ProcessingSummary?.UsersSkipped > 0)
            {
                ShowProcessingInfo = true;
                _logger.LogWarning(
                    "Working hours processing - No users processed, {Count} skipped (insufficient records)",
                    ProcessingSummary.UsersSkipped);
            }

            // Get all working hours records
            var allRecords = await _attendanceService.GetAllWorkingHoursAsync();
            var allUsers = await _userDatabaseService.GetAllUsersAsync();

            var userDictionary = allUsers.ToDictionary(u => u.UserId, u => u.Name);
            Users = allUsers
            .Select(u => new UserDropdownItem
             {
                 UserId = u.UserId,
                 UserName = u.Name
             })
            .OrderBy(u => u.UserName)
             .ToList();
            // Extract unique user IDs for filter dropdown
            UniqueUserIds = allRecords
                .Select(r => r.UserId)
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            // Set default dates if not provided
            if (!StartDate.HasValue && !EndDate.HasValue)
            {
                StartDate = DateTime.Today.AddMonths(-1);
                EndDate = DateTime.Today;
            }

            // Apply filters
            var filteredRecords = allRecords.AsEnumerable();

            // Date range filter
            if (StartDate.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.LoginDate.Date >= StartDate.Value.Date);
            }

            if (EndDate.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.LoginDate.Date <= EndDate.Value.Date);
            }

            // User filter
            if (!string.IsNullOrWhiteSpace(FilterUserId))
            {
                filteredRecords = filteredRecords.Where(r => r.UserId == FilterUserId);
            }

            var records = filteredRecords
                .OrderByDescending(r => r.LoginDate)
                .ThenBy(r => r.UserId)
                .ToList();

            // Populate view based on ViewType
            if (ViewType == "weekly")
            {
                await PopulateWeeklyView(records, userDictionary);
            }
            else if (ViewType == "monthly")
            {
                await PopulateMonthlyView(records, userDictionary);
            }
            else // daily (default)
            {
                await PopulateDailyView(records, userDictionary);
            }

            // Calculate overall statistics
            TotalRecords = records.Count;
            TotalHours = records.Sum(r => r.WorkingHours);
            AverageHours = TotalRecords > 0 ? TotalHours / TotalRecords : 0;
            UniqueUsers = records.Select(r => r.UserId).Distinct().Count();

            // Set status message
            if (StartDate.HasValue || EndDate.HasValue || !string.IsNullOrWhiteSpace(FilterUserId))
            {
                StatusMessage = $"Total: {TotalHours:F2} hours, Avg: {AverageHours:F2} hours ({ViewType} view)";
            }
            else
            {
                StatusMessage = $"Total: {TotalHours:F2} hours ({ViewType} view)";
            }

            _logger.LogInformation("User working hours records retrieved. Count: {Count}, ViewType: {ViewType}", TotalRecords, ViewType);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading working hours records: {ex.Message}";
            _logger.LogError(ex, "Error retrieving user working hours records");
        }
    }

    private async Task PopulateDailyView(List<UserWorkingHours> records, Dictionary<string, string> userDictionary)
    {
        WorkingHoursRecords = records
            .Select(r => new UserWorkingHoursViewModel
            {
                Id = r.Id,
                UserId = r.UserId,
                Name = userDictionary.ContainsKey(r.UserId) ? userDictionary[r.UserId] : "Unknown",
                LoginDate = r.LoginDate,
                LoginDateFormatted = r.LoginDate.ToString("dddd, MMMM dd, yyyy"),
                WorkingHours = r.WorkingHours,
                WorkingHoursFormatted = $"{r.WorkingHours:F2} hours"
            })
            .ToList();
    }

    private async Task PopulateWeeklyView(List<UserWorkingHours> records, Dictionary<string, string> userDictionary)
    {
        if (StartDate == null || EndDate == null)
            return;

        var weeklySummaries = await _userWorkingHoursService.GetWeeklyWorkingHoursSummaryAsync(
            StartDate.Value, EndDate.Value, FilterUserId);

        WeeklyWorkingHours = weeklySummaries
            .Select(w => new WeeklyWorkingHoursViewModel
            {
                Year = w.Year,
                Week = w.Week,
                WeekStart = w.WeekStart,
                WeekEnd = w.WeekEnd,
                WeekPeriod = $"Week {w.Week} ({w.WeekStart:MMM dd} - {w.WeekEnd:MMM dd})",
                UserId = w.UserId,
                UserName = userDictionary.ContainsKey(w.UserId) ? userDictionary[w.UserId] : "Unknown",
                TotalHours = w.TotalHours,
                DaysWorked = w.DaysWorked,
                AverageHoursPerDay = w.AverageHoursPerDay,
                TotalHoursFormatted = $"{w.TotalHours:F2} hrs",
                AverageFormatted = $"{w.AverageHoursPerDay:F2} hrs/day"
            })
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.Week)
            .ThenBy(w => w.UserId)
            .ToList();
    }

    private async Task PopulateMonthlyView(List<UserWorkingHours> records, Dictionary<string, string> userDictionary)
    {
        if (StartDate == null || EndDate == null)
            return;

        var monthlySummaries = await _userWorkingHoursService.GetMonthlyWorkingHoursSummaryAsync(
            StartDate.Value, EndDate.Value, FilterUserId);

        MonthlyWorkingHours = monthlySummaries
            .Select(m => new MonthlyWorkingHoursViewModel
            {
                Year = m.Year,
                Month = m.Month,
                MonthName = m.MonthName,
                MonthPeriod = $"{m.MonthName} {m.Year}",
                UserId = m.UserId,
                UserName = userDictionary.ContainsKey(m.UserId) ? userDictionary[m.UserId] : "Unknown",
                TotalHours = m.TotalHours,
                DaysWorked = m.DaysWorked,
                AverageHoursPerDay = m.AverageHoursPerDay,
                TotalHoursFormatted = $"{m.TotalHours:F2} hrs",
                AverageFormatted = $"{m.AverageHoursPerDay:F2} hrs/day"
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ThenBy(m => m.UserId)
            .ToList();
    }

    private async Task LoadWorkingHoursData()
    {
        // Your logic to fetch working hours data based on:
        // - StartDate
        // - EndDate
        // - FilterUserId
        // - ViewType

        // Populate WorkingHoursRecords, WeeklyWorkingHours, MonthlyWorkingHours based on ViewType

        var allRecords = await _attendanceService.GetAllWorkingHoursAsync();
        var allUsers = await _userDatabaseService.GetAllUsersAsync();

        var userDictionary = allUsers.ToDictionary(u => u.UserId, u => u.Name);

        // Store the user ID to name mapping for the filter dropdown
        UserIdToNameMap = userDictionary;

        // Extract unique user IDs for filter dropdown
        UniqueUserIds = allRecords
            .Select(r => r.UserId)
            .Distinct()
            .OrderBy(u => u)
            .ToList();

        // Set default dates if not provided
        if (!StartDate.HasValue && !EndDate.HasValue)
        {
            StartDate = DateTime.Today.AddMonths(-1);
            EndDate = DateTime.Today;
        }

        // Apply filters
        var filteredRecords = allRecords.AsEnumerable();

        // Date range filter
        if (StartDate.HasValue)
        {
            filteredRecords = filteredRecords.Where(r => r.LoginDate.Date >= StartDate.Value.Date);
        }

        if (EndDate.HasValue)
        {
            filteredRecords = filteredRecords.Where(r => r.LoginDate.Date <= EndDate.Value.Date);
        }

        // User filter
        if (!string.IsNullOrWhiteSpace(FilterUserId))
        {
            filteredRecords = filteredRecords.Where(r => r.UserId == FilterUserId);
        }

        var records = filteredRecords
            .OrderByDescending(r => r.LoginDate)
            .ThenBy(r => r.UserId)
            .ToList();

        // Populate view based on ViewType
        if (ViewType == "weekly")
        {
            await PopulateWeeklyView(records, userDictionary);
        }
        else if (ViewType == "monthly")
        {
            await PopulateMonthlyView(records, userDictionary);
        }
        else // daily (default)
        {
            await PopulateDailyView(records, userDictionary);
        }

        // Calculate overall statistics
        TotalRecords = records.Count;
        TotalHours = records.Sum(r => r.WorkingHours);
        AverageHours = TotalRecords > 0 ? TotalHours / TotalRecords : 0;
        UniqueUsers = records.Select(r => r.UserId).Distinct().Count();

        // Set status message
        if (StartDate.HasValue || EndDate.HasValue || !string.IsNullOrWhiteSpace(FilterUserId))
        {
            StatusMessage = $"Total: {TotalHours:F2} hours, Avg: {AverageHours:F2} hours ({ViewType} view)";
        }
        else
        {
            StatusMessage = $"Total: {TotalHours:F2} hours ({ViewType} view)";
        }

        _logger.LogInformation("User working hours records retrieved. Count: {Count}, ViewType: {ViewType}", TotalRecords, ViewType);
    }
    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("User {User} logging out", User.Identity?.Name);
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToPage("/Login");
    }
}
public class UserDropdownItem
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
public class UserWorkingHoursViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime LoginDate { get; set; }
    public string LoginDateFormatted { get; set; } = string.Empty;
    public decimal WorkingHours { get; set; }
    public string WorkingHoursFormatted { get; set; } = string.Empty;
}

public class DailyWorkingHoursViewModel
{
    public DateTime Date { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal WorkingHours { get; set; }
}

public class WeeklyWorkingHoursViewModel
{
    public int Year { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string WeekPeriod { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public int DaysWorked { get; set; }
    public decimal AverageHoursPerDay { get; set; }
    public string TotalHoursFormatted { get; set; } = string.Empty;
    public string AverageFormatted { get; set; } = string.Empty;
}

public class MonthlyWorkingHoursViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string MonthPeriod { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public int DaysWorked { get; set; }
    public decimal AverageHoursPerDay { get; set; }
    public string TotalHoursFormatted { get; set; } = string.Empty;
    public string AverageFormatted { get; set; } = string.Empty;
}
