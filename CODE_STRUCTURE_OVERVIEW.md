# Working Hours Calculation Service - Code Structure Overview

## Directory Structure

```
FaceRecognition.Api/
├── Models/
│   ├── Attendance.cs (existing)
│   └── UserWorkingHours.cs (new)
│
├── Services/
│   ├── IAttendanceService.cs (updated - added working hours methods)
│   ├── AttendanceService.cs (updated - added working hours implementation)
│   ├── IUserWorkingHoursService.cs (new - main interface)
│   └── UserWorkingHoursService.cs (new - implementation)
│
├── Controllers/
│   ├── AttendanceController.cs (existing)
│   └── WorkingHoursController.cs (new)
│
├── Pages/
│   ├── Index.cshtml (existing)
│   └── AttendanceList.cshtml (existing)
│
└── Program.cs (updated - service registration)
```

## Class Dependencies

```
WorkingHoursController
    ↓
IUserWorkingHoursService
    ↓
UserWorkingHoursService
    ↓
IAttendanceService
    ↓
AttendanceService
    ↓
SQLite Database
```

## Core Classes

### 1. IUserWorkingHoursService (Interface)

**Namespace**: `FaceRecognitionApp.Api.Services`

**Methods**:
```csharp
// Process all users for a date
Task<WorkingHoursProcessingSummary> ProcessUserWorkingHoursAsync(DateTime date);

// Process specific user for a date
Task<decimal?> ProcessUserWorkingHoursForUserAsync(string userId, DateTime date);
```

**Data Models**:
```csharp
WorkingHoursProcessingSummary
├── DateTime ProcessedDate
├── int TotalUsersProcessed
├── int UsersSkipped
├── List<UserWorkingHoursSummary> ProcessedRecords
└── List<string> SkippedUserIds

UserWorkingHoursSummary
├── string UserId
├── decimal WorkingHours
├── int RecordsProcessed
├── DateTime FirstScanTime
└── DateTime LastScanTime
```

### 2. UserWorkingHoursService (Implementation)

**Namespace**: `FaceRecognitionApp.Api.Services`

**Constructor**:
```csharp
public UserWorkingHoursService(
    IAttendanceService attendanceService,
    ILogger<UserWorkingHoursService> logger)
```

**Key Methods**:
1. `ProcessUserWorkingHoursAsync(date)` - Main processing method
2. `ProcessUserWorkingHoursForUserAsync(userId, date)` - Single user processing
3. `InsertUserWorkingHoursAsync(userId, date, hours)` - Database insertion

**Processing Steps**:
```
1. Get unprocessed attendance records
2. Filter by date
3. Group by UserId
4. For each user group:
   a. Check if count >= 2
   b. Calculate working hours
   c. Insert result
   d. Mark as processed
5. Collect and return summary
```

### 3. WorkingHoursController (API)

**Namespace**: `FaceRecognitionApp.Api.Controllers`

**Route**: `/api/workingHours`

**Endpoints**:
```
POST /api/workingHours/process
  Query: date (optional, yyyy-MM-dd)
  Returns: WorkingHoursProcessingSummary (200 OK)

POST /api/workingHours/process/{userId}
  Route: userId (required)
  Query: date (optional, yyyy-MM-dd)
  Returns: { userId, date, workingHours } (200 OK)
           or error message (400 Bad Request)
```

### 4. UserWorkingHours (Model)

**Namespace**: `FaceRecognitionApp.Api.Models`

**Properties**:
```csharp
public int Id { get; set; }
public string UserId { get; set; }
public DateTime LoginDate { get; set; }
public decimal WorkingHours { get; set; }
```

## Database Schema

### UserWorkingHours Table

```sql
CREATE TABLE IF NOT EXISTS UserWorkingHours (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    LoginDate TEXT NOT NULL,
    WorkingHours REAL NOT NULL,
    UNIQUE(UserId, LoginDate)
);

CREATE INDEX idx_working_hours_userid ON UserWorkingHours(UserId);
CREATE INDEX idx_working_hours_logindate ON UserWorkingHours(LoginDate);
```

## Configuration (Program.cs)

```csharp
// Service registration
builder.Services.AddSingleton<IUserWorkingHoursService, UserWorkingHoursService>();

// Razor Pages
builder.Services.AddRazorPages();

// Controllers
builder.Services.AddControllers();

// Mapping
app.MapControllers();
app.MapRazorPages();
```

## Data Flow Diagram

```
API Request
    ↓
WorkingHoursController
    ↓
    ├─→ Validate Input
    │   └─→ Return 400 if invalid
    │
    ├─→ Call IUserWorkingHoursService
    │   ↓
    │   UserWorkingHoursService
    │   ├─→ Call IAttendanceService.GetUnprocessedAttendanceAsync()
    │   ├─→ Filter records by date
    │   ├─→ Group by UserId
    │   ├─→ For each group:
    │   │   ├─→ Check if records >= 2
    │   │   ├─→ Calculate: WorkingHours = (Max - Min).TotalHours
    │   │   ├─→ Call IAttendanceService.InsertOrUpdateWorkingHoursAsync()
    │   │   ├─→ Call IAttendanceService.MarkAsProcessedAsync()
    │   │   └─→ Add to summary
    │   └─→ Return summary
    │
    └─→ Return 200 with summary
```

## Method Call Sequence

### Process All Users

```
Controller.ProcessWorkingHoursAsync(date)
  │
  ├─→ Service.ProcessUserWorkingHoursAsync(date)
  │     │
  │     ├─→ AttendanceService.GetUnprocessedAttendanceAsync()
  │     │
  │     ├─→ Filter & Group by UserId
  │     │
  │     └─→ For each user:
  │           │
  │           ├─→ Service.ProcessUserWorkingHoursForUserAsync(userId, date)
  │           │     │
  │           │     ├─→ AttendanceService.GetAttendanceByUserIdAsync(userId)
  │           │     │
  │           │     ├─→ Filter unprocessed for date
  │           │     │
  │           │     ├─→ Calculate working hours
  │           │     │
  │           │     └─→ InsertUserWorkingHoursAsync()
  │           │           │
  │           │           └─→ AttendanceService.InsertOrUpdateWorkingHoursAsync()
  │           │
  │           └─→ For each record:
  │                 └─→ AttendanceService.MarkAsProcessedAsync(id)
  │
  └─→ Return summary
```

## Error Handling Flow

```
Try
├─→ Get Records
├─→ Filter/Group
├─→ Process Each User
│   ├─→ Validate record count
│   ├─→ Calculate hours
│   ├─→ Insert to DB
│   └─→ Mark as processed
│
Catch (Exception ex)
├─→ Log error with context
├─→ Return appropriate status code
└─→ Include error message in response
```

## Response Examples

### Success - All Users

```json
{
  "processedDate": "2024-01-15T00:00:00",
  "totalUsersProcessed": 5,
  "usersSkipped": 2,
  "processedRecords": [
    {
      "userId": "user001",
      "workingHours": 8.5,
      "recordsProcessed": 2,
      "firstScanTime": "2024-01-15T09:00:00",
      "lastScanTime": "2024-01-15T17:30:00"
    }
  ],
  "skippedUserIds": ["user003", "user004"]
}
```

### Success - Single User

```json
{
  "userId": "user001",
  "date": "2024-01-15T00:00:00",
  "workingHours": 8.5
}
```

### Error - Insufficient Records

```
400 Bad Request
"Insufficient attendance records (minimum 2 required) for the specified user and date"
```

## Logging Output

```
INFO: Processing working hours for 5 users on 2024-01-15
WARN: User user003 has only 1 attendance record(s) on 2024-01-15. Skipping.
INFO: Processed working hours for UserId: user001, WorkingHours: 8.5, Records: 2, Date: 2024-01-15
INFO: Working hours processing complete. Processed: 5, Skipped: 2
```

## Code Quality Checklist

✅ **Null Safety** - Proper null checks throughout
✅ **Error Handling** - Try-catch with specific error messages
✅ **Logging** - INFO, WARN, DEBUG, ERROR levels
✅ **Async/Await** - Proper async method signatures
✅ **Dependency Injection** - Constructor injection used
✅ **Parameter Validation** - Input validation before processing
✅ **Database Safety** - Parameterized queries, transactions
✅ **Date Handling** - Normalized to date-only
✅ **Decimal Precision** - REAL type in database
✅ **Documentation** - XML comments on public methods

## Integration Points

```
AttendanceController ←─────┐
                           │
WorkingHoursController ────→ IUserWorkingHoursService
                           │
                           └─→ AttendanceService
                                 └─→ SQLite Database
```

## Next Steps for Integration

1. **Import Service** - Use dependency injection
2. **Call in Background Job** - For automatic daily processing
3. **Call in Web UI** - Add button to trigger manually
4. **Add Reporting** - Use UserWorkingHours table for reports
5. **Add Scheduling** - Use background service for automatic processing

## Example Integration Code

```csharp
// In your controller or service
public class MyService
{
    private readonly IUserWorkingHoursService _workingHoursService;
    
    public MyService(IUserWorkingHoursService workingHoursService)
    {
        _workingHoursService = workingHoursService;
    }
    
    public async Task ProcessDailyWorkingHours()
    {
        var summary = await _workingHoursService.ProcessUserWorkingHoursAsync(DateTime.Today);
        
        // Log summary
        logger.LogInformation(
            "Daily processing: {Processed} processed, {Skipped} skipped",
            summary.TotalUsersProcessed,
            summary.UsersSkipped);
    }
}
```

---

**Complete and ready for production deployment!**
