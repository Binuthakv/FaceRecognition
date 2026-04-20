namespace FaceRecognitionApp.Api.Models;

/// <summary>
/// Represents working hours statistics for a period.
/// </summary>
public class WorkingHoursStatistics
{
    public string UserId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public int RecordsCount { get; set; }
    public decimal AverageHoursPerDay { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Daily working hours summary.
/// </summary>
public class DailyWorkingHours
{
    public DateTime Date { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal WorkingHours { get; set; }
}

/// <summary>
/// Weekly working hours summary.
/// </summary>
public class WeeklyWorkingHours
{
    public int Year { get; set; }
    public int Week { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string UserId { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public int DaysWorked { get; set; }
    public decimal AverageHoursPerDay { get; set; }
}

/// <summary>
/// Monthly working hours summary.
/// </summary>
public class MonthlyWorkingHours
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public int DaysWorked { get; set; }
    public decimal AverageHoursPerDay { get; set; }
}

/// <summary>
/// Filter parameters for working hours queries.
/// </summary>
public class WorkingHoursFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// Working hours summary response.
/// </summary>
public class WorkingHoursSummaryResponse
{
    public List<UserWorkingHours> Records { get; set; } = new();
    public decimal TotalHours { get; set; }
    public decimal AverageHours { get; set; }
    public int UniqueUsers { get; set; }
    public int RecordsCount { get; set; }
}
