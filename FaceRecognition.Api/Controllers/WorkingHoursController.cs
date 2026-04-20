using FaceRecognitionApp.Api.Models;
using FaceRecognitionApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaceRecognitionApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkingHoursController : ControllerBase
{
    private readonly IUserWorkingHoursService _workingHoursService;
    private readonly ILogger<WorkingHoursController> _logger;

    public WorkingHoursController(
        IUserWorkingHoursService workingHoursService,
        ILogger<WorkingHoursController> logger)
    {
        _workingHoursService = workingHoursService;
        _logger = logger;
    }

    /// <summary>
    /// Process attendance records for a specific date and calculate working hours.
    /// Unprocessed records are grouped by user, and if more than one record exists,
    /// working hours are calculated. Records with only one scan are ignored.
    /// </summary>
    /// <param name="date">The date to process (format: yyyy-MM-dd). Defaults to today.</param>
    /// <returns>Summary of processed users and their calculated working hours.</returns>
    [HttpPost("process")]
    public async Task<ActionResult<WorkingHoursProcessingSummary>> ProcessWorkingHoursAsync(
        [FromQuery] DateTime? date = null)
    {
        try
        {
            var dateToProcess = date?.Date ?? DateTime.Today;
            _logger.LogInformation("Processing working hours for date: {Date}", dateToProcess);

            var result = await _workingHoursService.ProcessUserWorkingHoursAsync(dateToProcess);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing working hours");
            return StatusCode(500, $"Error processing working hours: {ex.Message}");
        }
    }

    /// <summary>
    /// Process working hours for a specific user on a specific date.
    /// </summary>
    /// <param name="userId">The user ID to process.</param>
    /// <param name="date">The date to process (format: yyyy-MM-dd). Defaults to today.</param>
    /// <returns>The calculated working hours, or 400 if insufficient records.</returns>
    [HttpPost("process/{userId}")]
    public async Task<ActionResult> ProcessUserWorkingHoursAsync(
        [FromRoute] string userId,
        [FromQuery] DateTime? date = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("UserId is required");

            var dateToProcess = date?.Date ?? DateTime.Today;
            _logger.LogInformation("Processing working hours for UserId: {UserId}, Date: {Date}",
                userId, dateToProcess);

            var workingHours = await _workingHoursService.ProcessUserWorkingHoursForUserAsync(userId, dateToProcess);

            if (!workingHours.HasValue)
            {
                return BadRequest("Insufficient attendance records (minimum 2 required) for the specified user and date");
            }

            return Ok(new { UserId = userId, Date = dateToProcess, WorkingHours = workingHours.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing working hours for UserId: {UserId}", userId);
            return StatusCode(500, $"Error processing working hours: {ex.Message}");
        }
    }

    /// <summary>
    /// Get working hours records with optional filtering by date range and user ID.
    /// </summary>
    /// <param name="startDate">Optional start date for filtering (format: yyyy-MM-dd).</param>
    /// <param name="endDate">Optional end date for filtering (format: yyyy-MM-dd).</param>
    /// <param name="userId">Optional user ID for filtering.</param>
    /// <returns>Working hours summary with filtered records and statistics.</returns>
    [HttpGet("summary")]
    public async Task<ActionResult<WorkingHoursSummaryResponse>> GetWorkingHoursSummaryAsync(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? userId = null)
    {
        try
        {
            _logger.LogInformation("Getting working hours summary. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var result = await _workingHoursService.GetWorkingHoursAsync(startDate, endDate, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting working hours summary");
            return StatusCode(500, $"Error getting working hours summary: {ex.Message}");
        }
    }

    /// <summary>
    /// Get daily working hours summary within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the range (format: yyyy-MM-dd). Required.</param>
    /// <param name="endDate">End date for the range (format: yyyy-MM-dd). Required.</param>
    /// <param name="userId">Optional user ID to filter by specific user.</param>
    /// <returns>List of daily working hours summaries.</returns>
    [HttpGet("daily")]
    public async Task<ActionResult<List<DailyWorkingHours>>> GetDailyWorkingHoursAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? userId = null)
    {
        try
        {
            if (startDate == default || endDate == default)
                return BadRequest("startDate and endDate are required");

            if (startDate > endDate)
                return BadRequest("startDate cannot be greater than endDate");

            _logger.LogInformation("Getting daily working hours. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var result = await _workingHoursService.GetDailyWorkingHoursSummaryAsync(startDate, endDate, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily working hours");
            return StatusCode(500, $"Error getting daily working hours: {ex.Message}");
        }
    }

    /// <summary>
    /// Get weekly working hours summary within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the range (format: yyyy-MM-dd). Required.</param>
    /// <param name="endDate">End date for the range (format: yyyy-MM-dd). Required.</param>
    /// <param name="userId">Optional user ID to filter by specific user.</param>
    /// <returns>List of weekly working hours summaries.</returns>
    [HttpGet("weekly")]
    public async Task<ActionResult<List<WeeklyWorkingHours>>> GetWeeklyWorkingHoursAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? userId = null)
    {
        try
        {
            if (startDate == default || endDate == default)
                return BadRequest("startDate and endDate are required");

            if (startDate > endDate)
                return BadRequest("startDate cannot be greater than endDate");

            _logger.LogInformation("Getting weekly working hours. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var result = await _workingHoursService.GetWeeklyWorkingHoursSummaryAsync(startDate, endDate, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly working hours");
            return StatusCode(500, $"Error getting weekly working hours: {ex.Message}");
        }
    }

    /// <summary>
    /// Get monthly working hours summary within a date range.
    /// </summary>
    /// <param name="startDate">Start date for the range (format: yyyy-MM-dd). Required.</param>
    /// <param name="endDate">End date for the range (format: yyyy-MM-dd). Required.</param>
    /// <param name="userId">Optional user ID to filter by specific user.</param>
    /// <returns>List of monthly working hours summaries.</returns>
    [HttpGet("monthly")]
    public async Task<ActionResult<List<MonthlyWorkingHours>>> GetMonthlyWorkingHoursAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? userId = null)
    {
        try
        {
            if (startDate == default || endDate == default)
                return BadRequest("startDate and endDate are required");

            if (startDate > endDate)
                return BadRequest("startDate cannot be greater than endDate");

            _logger.LogInformation("Getting monthly working hours. StartDate: {StartDate}, EndDate: {EndDate}, UserId: {UserId}",
                startDate, endDate, userId);

            var result = await _workingHoursService.GetMonthlyWorkingHoursSummaryAsync(startDate, endDate, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly working hours");
            return StatusCode(500, $"Error getting monthly working hours: {ex.Message}");
        }
    }
}
