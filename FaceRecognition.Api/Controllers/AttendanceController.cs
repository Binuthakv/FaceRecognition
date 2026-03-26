using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaceRecognitionApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Insert a new attendance record for a user scan.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="scanTime">The scan time (optional, defaults to current UTC time).</param>
    /// <returns>The ID of the inserted record.</returns>
    [HttpPost("record")]
    public async Task<ActionResult<int>> RecordAttendanceAsync(
        [FromQuery] string userId,
        [FromQuery] DateTime? scanTime = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("UserId is required");

        var time = scanTime ?? DateTime.UtcNow;

        try
        {
            var id = await _attendanceService.InsertAttendanceAsync(userId, time);
            _logger.LogInformation("Attendance recorded for UserId: {UserId}, Id: {AttendanceId}", userId, id);
            return Ok(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording attendance for UserId: {UserId}", userId);
            return StatusCode(500, $"Error recording attendance: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all attendance records.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Attendance>>> GetAllAttendanceAsync()
    {
        try
        {
            var records = await _attendanceService.GetAllAttendanceAsync();
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance records");
            return StatusCode(500, $"Error retrieving records: {ex.Message}");
        }
    }

    /// <summary>
    /// Get attendance records for a specific user.
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Attendance>>> GetAttendanceByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("UserId is required");

        try
        {
            var records = await _attendanceService.GetAttendanceByUserIdAsync(userId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance records for UserId: {UserId}", userId);
            return StatusCode(500, $"Error retrieving records: {ex.Message}");
        }
    }

    /// <summary>
    /// Get unprocessed attendance records.
    /// </summary>
    [HttpGet("unprocessed")]
    public async Task<ActionResult<List<Attendance>>> GetUnprocessedAttendanceAsync()
    {
        try
        {
            var records = await _attendanceService.GetUnprocessedAttendanceAsync();
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unprocessed attendance records");
            return StatusCode(500, $"Error retrieving records: {ex.Message}");
        }
    }

    /// <summary>
    /// Mark an attendance record as processed.
    /// </summary>
    [HttpPut("{attendanceId}/process")]
    public async Task<ActionResult> MarkAsProcessedAsync(int attendanceId)
    {
        try
        {
            await _attendanceService.MarkAsProcessedAsync(attendanceId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking attendance as processed. Id: {AttendanceId}", attendanceId);
            return StatusCode(500, $"Error processing record: {ex.Message}");
        }
    }
}
