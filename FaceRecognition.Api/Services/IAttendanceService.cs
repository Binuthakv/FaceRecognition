using FaceRecognitionApp.Api.Models;

namespace FaceRecognitionApp.Api.Services;

public interface IAttendanceService
{
    Task InitializeAsync();
    Task<int> InsertAttendanceAsync(string userId, DateTime scanTime);
    Task<List<Attendance>> GetAllAttendanceAsync();
    Task<List<Attendance>> GetAttendanceByUserIdAsync(string userId);
    Task<List<Attendance>> GetUnprocessedAttendanceAsync();
    Task MarkAsProcessedAsync(int attendanceId);
    Task<int> InsertOrUpdateWorkingHoursAsync(string userId, DateTime loginDate, decimal workingHours);
    Task<List<UserWorkingHours>> GetUserWorkingHoursAsync(string userId);
    Task<List<UserWorkingHours>> GetAllWorkingHoursAsync();
    Task<List<UserWorkingHours>> GetWorkingHoursByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task DeleteWorkingHoursAsync(int id);
}
