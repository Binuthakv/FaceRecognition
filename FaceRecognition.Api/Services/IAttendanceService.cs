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
}
