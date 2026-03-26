using System.Net;
using System.Net.Http.Json;
using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

/// <summary>
/// <see cref="IAttendanceService"/> implementation that delegates all
/// operations to the remote REST API (<c>api/attendance</c>).
/// </summary>
public class ApiAttendanceService : IAttendanceService
{
    private readonly HttpClient _http;

    public ApiAttendanceService(HttpClient http)
    {
        _http = http;
    }

    public async Task<int> RecordAttendanceAsync(string userId, DateTime scanTime)
    {
        try
        {
            var scanTimeStr = scanTime.ToString("O");
            var query = $"api/attendance/record?userId={Uri.EscapeDataString(userId)}&scanTime={Uri.EscapeDataString(scanTimeStr)}";
            var response = await _http.PostAsync(query, null);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<int>();
            return result;
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error recording attendance for UserId: {userId}", ex);
        }
    }

    public async Task<List<Attendance>> GetAllAttendanceAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Attendance>>("api/attendance") ?? [];
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Error retrieving all attendance records", ex);
        }
    }

    public async Task<List<Attendance>> GetAttendanceByUserIdAsync(string userId)
    {
        try
        {
            var url = $"api/attendance/user/{Uri.EscapeDataString(userId)}";
            return await _http.GetFromJsonAsync<List<Attendance>>(url) ?? [];
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error retrieving attendance for UserId: {userId}", ex);
        }
    }

    public async Task<List<Attendance>> GetUnprocessedAttendanceAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<Attendance>>("api/attendance/unprocessed") ?? [];
        }
        catch (Exception ex)
        {
            throw new HttpRequestException("Error retrieving unprocessed attendance records", ex);
        }
    }

    public async Task MarkAsProcessedAsync(int attendanceId)
    {
        try
        {
            var response = await _http.PutAsync($"api/attendance/{attendanceId}/process", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"Error marking attendance as processed. Id: {attendanceId}", ex);
        }
    }
}
