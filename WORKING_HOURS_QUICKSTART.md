# Working Hours Calculation Service - Quick Reference

## What It Does

Processes unprocessed attendance records and calculates working hours:
- Takes all unprocessed attendance records for a specific date
- Groups them by user
- If user has 2+ records: calculates working hours and saves result
- If user has 1 record: skips (not enough data)
- Marks processed records as Processed=true in the database

## Key Methods

### ProcessUserWorkingHoursAsync(date)
Processes **all users** for a given date.

```csharp
var summary = await workingHoursService.ProcessUserWorkingHoursAsync(DateTime.Today);

Console.WriteLine($"Processed: {summary.TotalUsersProcessed}");
Console.WriteLine($"Skipped: {summary.UsersSkipped}");
```

### ProcessUserWorkingHoursForUserAsync(userId, date)
Processes a **specific user** for a given date.

```csharp
var hours = await workingHoursService.ProcessUserWorkingHoursForUserAsync("user123", DateTime.Today);
if (hours.HasValue)
    Console.WriteLine($"Hours: {hours.Value}");
```

## API Endpoints

### Process All Users
```
POST /api/workingHours/process?date=2024-01-15
```

**Response:**
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
  "skippedUserIds": ["user456"]
}
```

### Process Single User
```
POST /api/workingHours/process/user123?date=2024-01-15
```

**Response:**
```json
{
  "userId": "user123",
  "date": "2024-01-15T00:00:00",
  "workingHours": 8.5
}
```

## How Working Hours Are Calculated

```
Working Hours = Last Scan Time - First Scan Time
```

**Example:**
- Login (9:00 AM) → Logout (5:30 PM) = **8.5 hours**

## Minimum Record Requirement

- ✅ **2+ records**: Processed and saved
- ❌ **1 record**: Skipped (insufficient data)
- ❌ **0 records**: Not applicable

## What Happens During Processing

1. Fetch all unprocessed attendance records for the date
2. Group by UserId
3. For each user:
   - If records < 2: Skip user, add to skipped list
   - If records ≥ 2:
     - Calculate: `WorkingHours = Last.ScanTime - First.ScanTime`
     - Insert into UserWorkingHours table
     - Mark all attendance records as Processed=true
4. Return summary with processed users and skipped users

## Files Created

| File | Purpose |
|------|---------|
| `IUserWorkingHoursService.cs` | Interface & data models |
| `UserWorkingHoursService.cs` | Service implementation |
| `WorkingHoursController.cs` | REST API endpoints |

## Database Tables

**Attendance** (existing)
- UserId, ScanTime, Processed

**UserWorkingHours** (new)
- UserId, LoginDate, WorkingHours

## Testing with cURL

```bash
# Process today for all users
curl -X POST https://localhost:7xxx/api/workingHours/process

# Process specific date for all users
curl -X POST "https://localhost:7xxx/api/workingHours/process?date=2024-01-15"

# Process specific user for today
curl -X POST https://localhost:7xxx/api/workingHours/process/user123

# Process specific user for specific date
curl -X POST "https://localhost:7xxx/api/workingHours/process/user123?date=2024-01-15"
```

## Return Values

### All Users Processing
Returns `WorkingHoursProcessingSummary`:
- ProcessedDate
- TotalUsersProcessed
- UsersSkipped
- ProcessedRecords (list with userId, workingHours, scanTimes)
- SkippedUserIds (list of users who had insufficient records)

### Single User Processing
Returns decimal (working hours) or null (insufficient records)

## Error Responses

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 400 | Bad request (missing userId, insufficient records) |
| 500 | Server error |

## Service is Auto-Registered

The service is automatically registered in `Program.cs`:
```csharp
builder.Services.AddSingleton<IUserWorkingHoursService, UserWorkingHoursService>();
```

No manual registration needed!

## Logging

The service logs:
- ✅ Successful processing
- ⚠️ Skipped users (insufficient records)
- ❌ Errors with context

Check application logs for details.
