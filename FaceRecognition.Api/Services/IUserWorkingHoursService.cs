using FaceRecognitionApp.Api.Models;

namespace FaceRecognitionApp.Api.Services;

public interface IUserWorkingHoursService
{
    /// <summary>
    /// Process attendance records for a specific day and calculate working hours.
    /// Unprocessed records are grouped by user, and if more than one record exists,
    /// working hours are calculated as the difference between the last and first scan time.
    /// Records with only one scan are ignored.
    /// </summary>
    /// <param name="date">The date to process attendance records for.</param>
    /// <returns>A summary of processed users and their calculated working hours.</returns>
    Task<WorkingHoursProcessingSummary> ProcessUserWorkingHoursAsync(DateTime date);

    /// <summary>
    /// Process attendance records for a specific user on a specific day.
    /// </summary>
    /// <param name="userId">The user ID to process.</param>
    /// <param name="date">The date to process.</param>
    /// <returns>The calculated working hours for the user, or null if insufficient records.</returns>
    Task<decimal?> ProcessUserWorkingHoursForUserAsync(string userId, DateTime date);

    /// <summary>
    /// Get working hours records with optional filtering by date range and user ID.
    /// </summary>
    /// <param name="startDate">Optional start date for filtering.</param>
    /// <param name="endDate">Optional end date for filtering.</param>
    /// <param name="userId">Optional user ID for filtering.</param>
    /// <returns>Working hours summary with filtered records.</returns>
    Task<WorkingHoursSummaryResponse> GetWorkingHoursAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null);

    /// <summary>
    /// Get daily working hours summary for all users within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the range.</param>
    /// <param name="endDate">End date for the range.</param>
    /// <param name="userId">Optional user ID to filter by specific user.</param>
    /// <returns>Daily working hours summaries.</returns>
    Task<List<DailyWorkingHours>> GetDailyWorkingHoursSummaryAsync(DateTime startDate, DateTime endDate, string? userId = null);

    /// <summary>
    /// Get weekly working hours summary for all users within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the range.</param>
    /// <param name="endDate">End date for the range.</param>
    /// <param name="userId">Optional user ID to filter by specific user.</param>
    /// <returns>Weekly working hours summaries.</returns>
    Task<List<WeeklyWorkingHours>> GetWeeklyWorkingHoursSummaryAsync(DateTime startDate, DateTime endDate, string? userId = null);

    /// <summary>
    /// Get monthly working hours summary for all users within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the range.</param>
    /// <param name="endDate">End date for the range.</param>
    /// <param name="userId">Optional user ID to filter by specific user.</param>
    /// <returns>Monthly working hours summaries.</returns>
    Task<List<MonthlyWorkingHours>> GetMonthlyWorkingHoursSummaryAsync(DateTime startDate, DateTime endDate, string? userId = null);
}

public class WorkingHoursProcessingSummary
{
    public DateTime ProcessedDate { get; set; }
    public int TotalUsersProcessed { get; set; }
    public int UsersSkipped { get; set; }
    public List<UserWorkingHoursSummary> ProcessedRecords { get; set; } = new();
    public List<string> SkippedUserIds { get; set; } = new();
}

public class UserWorkingHoursSummary
{
    public string UserId { get; set; } = string.Empty;
    public decimal WorkingHours { get; set; }
    public int RecordsProcessed { get; set; }
    public DateTime FirstScanTime { get; set; }
    public DateTime LastScanTime { get; set; }
}
