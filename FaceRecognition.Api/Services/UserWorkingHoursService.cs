using FaceRecognitionApp.Api.Models;

namespace FaceRecognitionApp.Api.Services;

public class UserWorkingHoursService : IUserWorkingHoursService
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<UserWorkingHoursService> _logger;

    public UserWorkingHoursService(
        IAttendanceService attendanceService,
        ILogger<UserWorkingHoursService> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    public async Task<WorkingHoursProcessingSummary> ProcessUserWorkingHoursAsync(DateTime date)
    {
        var summary = new WorkingHoursProcessingSummary
        {
            ProcessedDate = date.Date
        };

        try
        {
            // Get all unprocessed attendance records
            var unprocessedRecords = await _attendanceService.GetUnprocessedAttendanceAsync();

            // Filter records for the specified date
            var recordsForDate = unprocessedRecords
                .Where(r => r.ScanTime.Date == date.Date)
                .ToList();

            if (!recordsForDate.Any())
            {
                _logger.LogInformation("No unprocessed attendance records found for date: {Date}", date.Date);
                return summary;
            }

            // Group by UserId
            var groupedByUser = recordsForDate
                .GroupBy(r => r.UserId)
                .ToList();

            _logger.LogInformation("Processing working hours for {UserCount} users on {Date}",
                groupedByUser.Count, date.Date);

            // Process each user
            foreach (var userGroup in groupedByUser)
            {
                var userId = userGroup.Key;
                var userRecords = userGroup.ToList();

                // Skip if only one record
                if (userRecords.Count < 2)
                {
                    _logger.LogWarning("User {UserId} has only {RecordCount} attendance record(s) on {Date}. Skipping.",
                        userId, userRecords.Count, date.Date);
                    summary.SkippedUserIds.Add(userId);
                    summary.UsersSkipped++;
                    continue;
                }

                // Calculate working hours
                var workingHours = await ProcessUserWorkingHoursForUserAsync(userId, date);

                if (workingHours.HasValue)
                {
                    // Mark all records as processed
                    foreach (var record in userRecords)
                    {
                        await _attendanceService.MarkAsProcessedAsync(record.Id);
                    }

                    summary.TotalUsersProcessed++;
                    summary.ProcessedRecords.Add(new UserWorkingHoursSummary
                    {
                        UserId = userId,
                        WorkingHours = workingHours.Value,
                        RecordsProcessed = userRecords.Count,
                        FirstScanTime = userRecords.Min(r => r.ScanTime),
                        LastScanTime = userRecords.Max(r => r.ScanTime)
                    });

                    _logger.LogInformation(
                        "Processed working hours for UserId: {UserId}, WorkingHours: {Hours}, Records: {Count}, Date: {Date}",
                        userId, workingHours.Value, userRecords.Count, date.Date);
                }
            }

            _logger.LogInformation(
                "Working hours processing complete. Processed: {ProcessedCount}, Skipped: {SkippedCount}",
                summary.TotalUsersProcessed, summary.UsersSkipped);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing working hours for date: {Date}", date.Date);
            throw;
        }
    }

    public async Task<decimal?> ProcessUserWorkingHoursForUserAsync(string userId, DateTime date)
    {
        try
        {
            // Get all unprocessed attendance records for the user
            var userRecords = await _attendanceService.GetAttendanceByUserIdAsync(userId);

            // Filter unprocessed records for the specified date
            var recordsForDate = userRecords
                .Where(r => r.ScanTime.Date == date.Date && !r.Processed)
                .OrderBy(r => r.ScanTime)
                .ToList();

            if (recordsForDate.Count < 2)
            {
                _logger.LogWarning("User {UserId} has insufficient attendance records ({Count}) for {Date}",
                    userId, recordsForDate.Count, date.Date);
                return null;
            }

            // Calculate working hours
            var firstScan = recordsForDate.First().ScanTime;
            var lastScan = recordsForDate.Last().ScanTime;
            var workingHours = (decimal)(lastScan - firstScan).TotalHours;

            // Insert into UserWorkingHours table
            await InsertUserWorkingHoursAsync(userId, date, workingHours);

            return workingHours;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing working hours for UserId: {UserId}, Date: {Date}", userId, date.Date);
            throw;
        }
    }

    private async Task InsertUserWorkingHoursAsync(string userId, DateTime loginDate, decimal workingHours)
    {
        try
        {
            var recordId = await _attendanceService.InsertOrUpdateWorkingHoursAsync(userId, loginDate, workingHours);
            _logger.LogDebug("Inserted working hours record. Id: {Id}, UserId: {UserId}, Date: {Date}, Hours: {Hours}",
                recordId, userId, loginDate.Date, workingHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting working hours for UserId: {UserId}, Date: {Date}",
                userId, loginDate.Date);
            throw;
        }
    }

    public async Task<WorkingHoursSummaryResponse> GetWorkingHoursAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting working hours. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            // Get all working hours records
            List<UserWorkingHours> records;

            if (startDate.HasValue && endDate.HasValue)
            {
                records = await _attendanceService.GetWorkingHoursByDateRangeAsync(startDate.Value, endDate.Value);
            }
            else if (startDate.HasValue)
            {
                records = await _attendanceService.GetWorkingHoursByDateRangeAsync(startDate.Value, DateTime.Today.AddYears(1));
            }
            else if (endDate.HasValue)
            {
                records = await _attendanceService.GetWorkingHoursByDateRangeAsync(DateTime.MinValue, endDate.Value);
            }
            else
            {
                records = await _attendanceService.GetAllWorkingHoursAsync();
            }

            // Filter by user ID if provided
            if (!string.IsNullOrWhiteSpace(userId))
            {
                records = records.Where(r => r.UserId == userId).ToList();
            }

            // Calculate statistics
            var response = new WorkingHoursSummaryResponse
            {
                Records = records,
                RecordsCount = records.Count,
                TotalHours = records.Sum(r => r.WorkingHours),
                UniqueUsers = records.Select(r => r.UserId).Distinct().Count()
            };

            response.AverageHours = response.RecordsCount > 0 ? response.TotalHours / response.RecordsCount : 0;

            _logger.LogInformation("Retrieved {Count} working hours records", response.RecordsCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting working hours");
            throw;
        }
    }

    public async Task<List<DailyWorkingHours>> GetDailyWorkingHoursSummaryAsync(DateTime startDate, DateTime endDate, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting daily working hours summary. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var records = await _attendanceService.GetWorkingHoursByDateRangeAsync(startDate, endDate);

            // Filter by user ID if provided
            if (!string.IsNullOrWhiteSpace(userId))
            {
                records = records.Where(r => r.UserId == userId).ToList();
            }

            // Group by date and convert to DailyWorkingHours
            var dailyHours = records
                .GroupBy(r => new { r.LoginDate.Date, r.UserId })
                .Select(g => new DailyWorkingHours
                {
                    Date = g.Key.Date,
                    UserId = g.Key.UserId,
                    WorkingHours = g.Sum(r => r.WorkingHours)
                })
                .OrderBy(d => d.Date)
                .ThenBy(d => d.UserId)
                .ToList();

            return dailyHours;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily working hours summary");
            throw;
        }
    }

    public async Task<List<WeeklyWorkingHours>> GetWeeklyWorkingHoursSummaryAsync(DateTime startDate, DateTime endDate, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting weekly working hours summary. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var records = await _attendanceService.GetWorkingHoursByDateRangeAsync(startDate, endDate);

            // Filter by user ID if provided
            if (!string.IsNullOrWhiteSpace(userId))
            {
                records = records.Where(r => r.UserId == userId).ToList();
            }

            var weeklyHours = new List<WeeklyWorkingHours>();

            // Group by week and user
            var groupedByWeek = records
                .GroupBy(r => new
                {
                    Year = r.LoginDate.Year,
                    Week = GetWeekNumber(r.LoginDate),
                    UserId = r.UserId
                })
                .ToList();

            foreach (var group in groupedByWeek)
            {
                var daysWorked = group.Select(r => r.LoginDate.Date).Distinct().Count();
                var totalHours = group.Sum(r => r.WorkingHours);

                var weekStart = GetWeekStart(group.First().LoginDate);
                var weekEnd = weekStart.AddDays(6);

                weeklyHours.Add(new WeeklyWorkingHours
                {
                    Year = group.Key.Year,
                    Week = group.Key.Week,
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    UserId = group.Key.UserId,
                    TotalHours = totalHours,
                    DaysWorked = daysWorked,
                    AverageHoursPerDay = daysWorked > 0 ? totalHours / daysWorked : 0
                });
            }

            return weeklyHours.OrderBy(w => w.Year).ThenBy(w => w.Week).ThenBy(w => w.UserId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly working hours summary");
            throw;
        }
    }

    public async Task<List<MonthlyWorkingHours>> GetMonthlyWorkingHoursSummaryAsync(DateTime startDate, DateTime endDate, string? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting monthly working hours summary. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var records = await _attendanceService.GetWorkingHoursByDateRangeAsync(startDate, endDate);

            // Filter by user ID if provided
            if (!string.IsNullOrWhiteSpace(userId))
            {
                records = records.Where(r => r.UserId == userId).ToList();
            }

            var monthlyHours = new List<MonthlyWorkingHours>();

            // Group by month and user
            var groupedByMonth = records
                .GroupBy(r => new
                {
                    r.LoginDate.Year,
                    r.LoginDate.Month,
                    UserId = r.UserId
                })
                .ToList();

            foreach (var group in groupedByMonth)
            {
                var daysWorked = group.Select(r => r.LoginDate.Date).Distinct().Count();
                var totalHours = group.Sum(r => r.WorkingHours);
                var monthName = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMMM");

                monthlyHours.Add(new MonthlyWorkingHours
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    MonthName = monthName,
                    UserId = group.Key.UserId,
                    TotalHours = totalHours,
                    DaysWorked = daysWorked,
                    AverageHoursPerDay = daysWorked > 0 ? totalHours / daysWorked : 0
                });
            }

            return monthlyHours.OrderBy(m => m.Year).ThenBy(m => m.Month).ThenBy(m => m.UserId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly working hours summary");
            throw;
        }
    }

    private int GetWeekNumber(DateTime date)
    {
        var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = cultureInfo.Calendar;
        return calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = cultureInfo.Calendar;
        var firstDayOfWeek = cultureInfo.DateTimeFormat.FirstDayOfWeek;

        var diff = (int)date.DayOfWeek - (int)firstDayOfWeek;
        if (diff < 0)
            diff += 7;

        return date.AddDays(-diff).Date;
    }
}
