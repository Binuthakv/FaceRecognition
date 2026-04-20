using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaceRecognitionApp.Api.Pages;

public class AttendanceListModel : PageModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly ILogger<AttendanceListModel> _logger;

    public List<AttendanceRecordViewModel> AttendanceRecords { get; set; } = new();
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

            StatusMessage = $"Successfully loaded {AttendanceRecords.Count} attendance records.";
            _logger.LogInformation("Attendance records retrieved: {Count}", AttendanceRecords.Count);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading attendance records: {ex.Message}";
            _logger.LogError(ex, "Error retrieving attendance records");
        }
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
