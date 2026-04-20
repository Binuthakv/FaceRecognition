# Working Hours Calculation Service

## Overview
A comprehensive service for calculating user working hours from attendance records. The service processes unprocessed attendance records for a specific date, groups them by user, calculates working hours, and updates the database.

## Key Features

✅ **Automatic Processing** - Calculates working hours from attendance records
✅ **Minimum Records Check** - Requires at least 2 records per user to calculate (ignores single-record days)
✅ **Batch Processing** - Processes all users for a given date in one call
✅ **Automatic Mark as Processed** - Updates attendance records as processed after calculation
✅ **Comprehensive Logging** - Detailed logging of all operations
✅ **Error Handling** - Graceful error handling with detailed messages
✅ **API Endpoints** - RESTful endpoints for triggering calculations

## Architecture

### Services & Interfaces

**IUserWorkingHoursService**
- `ProcessUserWorkingHoursAsync(DateTime date)` - Process all users for a date
- `ProcessUserWorkingHoursForUserAsync(string userId, DateTime date)` - Process specific user

**UserWorkingHoursService** (Implementation)
- Depends on `IAttendanceService`
- Coordinates between attendance data and working hours calculations

**WorkingHoursController**
- REST API endpoints for triggering calculations
- Returns processing summaries

## Data Flow

```
Unprocessed Attendance Records
    ↓
Group by UserId & Date
    ↓
Check Record Count (min 2)
    ↓
For each valid user:
  - Calculate hours (last ScanTime - first ScanTime)
  - Insert into UserWorkingHours table
  - Mark attendance records as Processed=true
    ↓
Return Processing Summary
```

## API Endpoints

### 1. Process Working Hours for All Users (by Date)

**POST** `/api/workingHours/process`

**Query Parameters:**
- `date` (optional) - Date to process (format: yyyy-MM-dd, defaults to today)

**Example:**
```bash
POST /api/workingHours/process?date=2024-01-15
```

**Response (200 OK):**
```json
{
  "processedDate": "2024-01-15T00:00:00",
  "totalUsersProcessed": 5,
  "usersSkipped": 2,
  "processedRecords": [
    {
      "userId": "user123",
      "workingHours": 8.5,
      "recordsProcessed": 2,
      "firstScanTime": "2024-01-15T09:00:00",
      "lastScanTime": "2024-01-15T17:30:00"
    }
  ],
  "skippedUserIds": ["user456", "user789"]
}
```

### 2. Process Working Hours for Specific User

**POST** `/api/workingHours/process/{userId}`

**Route Parameters:**
- `userId` (required) - The user ID to process

**Query Parameters:**
- `date` (optional) - Date to process (format: yyyy-MM-dd, defaults to today)

**Example:**
```bash
POST /api/workingHours/process/user123?date=2024-01-15
```

**Response (200 OK):**
```json
{
  "userId": "user123",
  "date": "2024-01-15T00:00:00",
  "workingHours": 8.5
}
```

**Response (400 Bad Request):**
```json
"Insufficient attendance records (minimum 2 required) for the specified user and date"
```

## Calculation Logic

### Working Hours Formula
```
WorkingHours = (LastScanTime - FirstScanTime).TotalHours
```

### Example
- **User**: user123
- **Date**: 2024-01-15
- **Attendance Records**:
  1. 09:00:00 (Login)
  2. 12:30:00 (Lunch out)
  3. 13:30:00 (Lunch back)
  4. 17:30:00 (Logout)

**Calculation**: 17:30:00 - 09:00:00 = 8.5 hours

**Result**: UserWorkingHours record with 8.5 hours for user123 on 2024-01-15

## Record Processing Behavior

### ✅ Processed Records
- User has 2 or more attendance records on the date
- Working hours are calculated
- Result inserted into UserWorkingHours table
- All attendance records marked as Processed=true

### ⏭️ Skipped Records
- User has only 1 attendance record on the date
- Not enough data to calculate working hours
- User ID added to `SkippedUserIds` list
- Attendance record remains Processed=false

## Database Schema

### Attendance Table
```sql
CREATE TABLE Attendance (
    Id INTEGER PRIMARY KEY,
    UserId TEXT NOT NULL,
    ScanTime TEXT NOT NULL,
    Processed INTEGER NOT NULL DEFAULT 0
)
```

### UserWorkingHours Table
```sql
CREATE TABLE UserWorkingHours (
    Id INTEGER PRIMARY KEY,
    UserId TEXT NOT NULL,
    LoginDate TEXT NOT NULL,
    WorkingHours REAL NOT NULL,
    UNIQUE(UserId, LoginDate)
)
```

## Service Registration

The service is automatically registered in `Program.cs`:
```csharp
builder.Services.AddSingleton<IUserWorkingHoursService, UserWorkingHoursService>();
```

## Usage Examples

### C# Code
```csharp
// Inject the service
private readonly IUserWorkingHoursService _workingHoursService;

// Process all users for a specific date
var summary = await _workingHoursService.ProcessUserWorkingHoursAsync(
    new DateTime(2024, 1, 15));

Console.WriteLine($"Processed: {summary.TotalUsersProcessed}");
Console.WriteLine($"Skipped: {summary.UsersSkipped}");

// Process specific user
var workingHours = await _workingHoursService.ProcessUserWorkingHoursForUserAsync(
    "user123", 
    new DateTime(2024, 1, 15));

if (workingHours.HasValue)
{
    Console.WriteLine($"Working Hours: {workingHours.Value}");
}
```

### cURL Examples
```bash
# Process all users for today
curl -X POST https://localhost:7xxx/api/workingHours/process

# Process all users for a specific date
curl -X POST "https://localhost:7xxx/api/workingHours/process?date=2024-01-15"

# Process specific user for today
curl -X POST https://localhost:7xxx/api/workingHours/process/user123

# Process specific user for a specific date
curl -X POST "https://localhost:7xxx/api/workingHours/process/user123?date=2024-01-15"
```

## Logging

The service provides detailed logging at different levels:

**Information Level:**
- Working hours processing start and completion
- Records successfully inserted into UserWorkingHours table
- Processing summary (processed count, skipped count)

**Warning Level:**
- Users with insufficient records (skipped)

**Debug Level:**
- Database operations
- Record insertions

**Error Level:**
- Processing failures
- Database errors

## Error Handling

The service handles various error scenarios:

1. **Insufficient Records** - Returns null if less than 2 records
2. **Database Errors** - Logs and throws with context
3. **Invalid Dates** - Uses date without time component
4. **Missing Users** - Logs and continues processing

## Files Created

1. **FaceRecognition.Api/Services/IUserWorkingHoursService.cs**
   - Interface and data models

2. **FaceRecognition.Api/Services/UserWorkingHoursService.cs**
   - Service implementation

3. **FaceRecognition.Api/Controllers/WorkingHoursController.cs**
   - REST API endpoints

## Dependencies

- `IAttendanceService` - For accessing attendance records
- `ILogger<T>` - For logging operations
- `.NET 10` - Target framework
- `Microsoft.Data.Sqlite` - Database access (via AttendanceService)

## Notes

- All dates are normalized to date-only (time component removed)
- Working hours are stored as REAL (decimal) in the database
- The UNIQUE constraint on (UserId, LoginDate) ensures one record per user per day
- Records are processed in chronological order
- The service is stateless and thread-safe (uses semaphore locks in AttendanceService)

## Future Enhancements

Potential improvements:
1. Break times calculation (lunch, breaks)
2. Overtime calculations
3. Scheduled processing (background job)
4. Attendance pattern analysis
5. Reports generation
6. Holiday/weekend handling
7. Configurable working hours threshold
