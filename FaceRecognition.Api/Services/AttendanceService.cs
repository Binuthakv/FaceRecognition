using FaceRecognitionApp.Api.Models;
using Microsoft.Data.Sqlite;

namespace FaceRecognitionApp.Api.Services;

public class AttendanceService : IAttendanceService, IDisposable
{
    private SqliteConnection? _connection;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(ILogger<AttendanceService> logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FaceRecognitionDB.db");
        _logger.LogInformation("AttendanceService created. Database path: {Path}", _dbPath);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _logger.LogDebug("Initializing attendance database connection...");

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            await _connection.OpenAsync();

            await CreateTableAsync();
            _initialized = true;

            _logger.LogInformation("Attendance database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize attendance database");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task CreateTableAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = @"
            CREATE TABLE IF NOT EXISTS Attendance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                ScanTime TEXT NOT NULL,
                Processed INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_attendance_userid ON Attendance(UserId);
            CREATE INDEX IF NOT EXISTS idx_attendance_processed ON Attendance(Processed);

            CREATE TABLE IF NOT EXISTS UserWorkingHours (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                LoginDate TEXT NOT NULL,
                WorkingHours REAL NOT NULL,
                UNIQUE(UserId, LoginDate)
            );

            CREATE INDEX IF NOT EXISTS idx_working_hours_userid ON UserWorkingHours(UserId);
            CREATE INDEX IF NOT EXISTS idx_working_hours_logindate ON UserWorkingHours(LoginDate);
        ";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();

        _logger.LogDebug("Attendance and UserWorkingHours tables created or verified");
    }

    public async Task<int> InsertAttendanceAsync(string userId, DateTime scanTime)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = @"
            INSERT INTO Attendance (UserId, ScanTime, Processed)
            VALUES (@userId, @scanTime, 0);
            SELECT last_insert_rowid();
        ";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@scanTime", scanTime.ToString("O"));

        var result = await command.ExecuteScalarAsync();
        var attendanceId = Convert.ToInt32(result);

        _logger.LogInformation("Attendance record inserted for UserId: {UserId}, ScanTime: {ScanTime}, Id: {Id}",
            userId, scanTime, attendanceId);

        return attendanceId;
    }

    public async Task<List<Attendance>> GetAllAttendanceAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "SELECT Id, UserId, ScanTime, Processed FROM Attendance ORDER BY ScanTime DESC";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Attendance>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Attendance
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                ScanTime = DateTime.Parse(reader.GetString(2)),
                Processed = reader.GetInt32(3) == 1
            });
        }

        return result;
    }

    public async Task<List<Attendance>> GetAttendanceByUserIdAsync(string userId)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "SELECT Id, UserId, ScanTime, Processed FROM Attendance WHERE UserId = @userId ORDER BY ScanTime DESC";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@userId", userId);

        var result = new List<Attendance>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Attendance
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                ScanTime = DateTime.Parse(reader.GetString(2)),
                Processed = reader.GetInt32(3) == 1
            });
        }

        return result;
    }

    public async Task<List<Attendance>> GetUnprocessedAttendanceAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "SELECT Id, UserId, ScanTime, Processed FROM Attendance WHERE Processed = 0 ORDER BY ScanTime ASC";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<Attendance>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new Attendance
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                ScanTime = DateTime.Parse(reader.GetString(2)),
                Processed = reader.GetInt32(3) == 1
            });
        }

        return result;
    }

    public async Task MarkAsProcessedAsync(int attendanceId)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "UPDATE Attendance SET Processed = 1 WHERE Id = @id";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", attendanceId);

        await command.ExecuteNonQueryAsync();

        _logger.LogDebug("Attendance record marked as processed. Id: {Id}", attendanceId);
    }

    /// <summary>
    /// Insert or update working hours for a user on a specific date.
    /// </summary>
    public async Task<int> InsertOrUpdateWorkingHoursAsync(string userId, DateTime loginDate, decimal workingHours)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = @"
            INSERT INTO UserWorkingHours (UserId, LoginDate, WorkingHours)
            VALUES (@userId, @loginDate, @workingHours)
            ON CONFLICT(UserId, LoginDate) DO UPDATE SET
                WorkingHours = @workingHours
            RETURNING Id;
        ";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@loginDate", loginDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@workingHours", workingHours);

        var result = await command.ExecuteScalarAsync();
        var recordId = Convert.ToInt32(result);

        _logger.LogInformation("Working hours inserted/updated for UserId: {UserId}, LoginDate: {LoginDate}, Hours: {Hours}",
            userId, loginDate.Date, workingHours);

        return recordId;
    }

    /// <summary>
    /// Get working hours for a specific user.
    /// </summary>
    public async Task<List<UserWorkingHours>> GetUserWorkingHoursAsync(string userId)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "SELECT Id, UserId, LoginDate, WorkingHours FROM UserWorkingHours WHERE UserId = @userId ORDER BY LoginDate DESC";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@userId", userId);

        var result = new List<UserWorkingHours>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new UserWorkingHours
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                LoginDate = DateTime.Parse(reader.GetString(2)),
                WorkingHours = reader.GetDecimal(3)
            });
        }

        return result;
    }

    /// <summary>
    /// Get all working hours records.
    /// </summary>
    public async Task<List<UserWorkingHours>> GetAllWorkingHoursAsync()
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "SELECT Id, UserId, LoginDate, WorkingHours FROM UserWorkingHours ORDER BY LoginDate DESC";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<UserWorkingHours>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new UserWorkingHours
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                LoginDate = DateTime.Parse(reader.GetString(2)),
                WorkingHours = reader.GetDecimal(3)
            });
        }

        return result;
    }

    /// <summary>
    /// Get working hours for a specific date range.
    /// </summary>
    public async Task<List<UserWorkingHours>> GetWorkingHoursByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = @"
            SELECT Id, UserId, LoginDate, WorkingHours 
            FROM UserWorkingHours 
            WHERE LoginDate >= @startDate AND LoginDate <= @endDate 
            ORDER BY LoginDate DESC, UserId ASC
        ";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@startDate", startDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd"));

        var result = new List<UserWorkingHours>();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new UserWorkingHours
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetString(1),
                LoginDate = DateTime.Parse(reader.GetString(2)),
                WorkingHours = reader.GetDecimal(3)
            });
        }

        return result;
    }

    /// <summary>
    /// Delete working hours record by ID.
    /// </summary>
    public async Task DeleteWorkingHoursAsync(int id)
    {
        if (_connection is null) throw new InvalidOperationException("Database not initialized");

        const string sql = "DELETE FROM UserWorkingHours WHERE Id = @id";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);

        await command.ExecuteNonQueryAsync();

        _logger.LogDebug("Working hours record deleted. Id: {Id}", id);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
