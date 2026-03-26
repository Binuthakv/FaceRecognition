using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

public interface IAttendanceService
{
    Task<int> RecordAttendanceAsync(string userId, DateTime scanTime);
    Task<List<Attendance>> GetAllAttendanceAsync();
    Task<List<Attendance>> GetAttendanceByUserIdAsync(string userId);
    Task<List<Attendance>> GetUnprocessedAttendanceAsync();
    Task MarkAsProcessedAsync(int attendanceId);
}
