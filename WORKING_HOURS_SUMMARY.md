# Working Hours Calculation Service - Summary

## 🎯 What Was Created

A complete working hours calculation service that:
1. ✅ Processes unprocessed attendance records for a specific date
2. ✅ Groups records by user
3. ✅ Calculates working hours (Last ScanTime - First ScanTime)
4. ✅ Saves results to UserWorkingHours table
5. ✅ Updates attendance records as processed
6. ✅ Provides detailed processing summaries
7. ✅ Includes REST API endpoints

## 📁 Files Created

### Services
```
FaceRecognition.Api/
├── Services/
│   ├── IUserWorkingHoursService.cs (Interface & Models)
│   └── UserWorkingHoursService.cs (Implementation)
├── Controllers/
│   └── WorkingHoursController.cs (REST API)
└── Models/
    └── UserWorkingHours.cs (Data Model - already created)
```

### Documentation
```
├── WORKING_HOURS_CALCULATION_GUIDE.md (Complete Guide)
└── WORKING_HOURS_QUICKSTART.md (Quick Reference)
```

## 🔄 Processing Flow

```
┌─────────────────────────────────────┐
│  Get Unprocessed Attendance Records │
│       for Specified Date            │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│     Group Records by UserId         │
└──────────────┬──────────────────────┘
               │
        ┌──────┴──────┐
        │             │
        ▼             ▼
   < 2 Records   ≥ 2 Records
        │             │
        │             ▼
        │    ┌──────────────────────┐
        │    │ Calculate Hours      │
        │    │ (Last - First)       │
        │    └─────────┬────────────┘
        │              │
        │              ▼
        │    ┌──────────────────────┐
        │    │  Insert Result into  │
        │    │ UserWorkingHours Tbl │
        │    └─────────┬────────────┘
        │              │
        │              ▼
        │    ┌──────────────────────┐
        │    │ Mark Records as      │
        │    │ Processed = true     │
        │    └─────────┬────────────┘
        │              │
        └──────┬───────┘
               │
               ▼
    ┌────────────────────────┐
    │  Return Processing     │
    │  Summary               │
    │ - Processed Count      │
    │ - Skipped Count        │
    │ - Details              │
    └────────────────────────┘
```

## 🚀 Quick Usage

### Via Service (C#)
```csharp
// Process all users for today
var summary = await workingHoursService.ProcessUserWorkingHoursAsync(DateTime.Today);

// Process specific user
var hours = await workingHoursService.ProcessUserWorkingHoursForUserAsync("user123", DateTime.Today);
```

### Via API (HTTP)
```bash
# Process all users
curl -X POST https://localhost:7xxx/api/workingHours/process?date=2024-01-15

# Process specific user
curl -X POST https://localhost:7xxx/api/workingHours/process/user123?date=2024-01-15
```

## 📊 Example Scenario

**Date:** January 15, 2024

| User | Attendance Records | Action | Result |
|------|-------------------|--------|--------|
| user001 | 09:00, 17:30 | Calculate | ✅ 8.5 hours |
| user002 | 09:00, 12:30, 13:30, 17:00 | Calculate | ✅ 8 hours |
| user003 | 09:00 | Skip | ⏭️ Only 1 record |
| user004 | (none) | Skip | ⏭️ No records |

**Processing Summary:**
```json
{
  "processedDate": "2024-01-15",
  "totalUsersProcessed": 2,
  "usersSkipped": 1,
  "processedRecords": [
    {
      "userId": "user001",
      "workingHours": 8.5,
      "recordsProcessed": 2,
      "firstScanTime": "2024-01-15T09:00:00",
      "lastScanTime": "2024-01-15T17:30:00"
    },
    {
      "userId": "user002",
      "workingHours": 8.0,
      "recordsProcessed": 4,
      "firstScanTime": "2024-01-15T09:00:00",
      "lastScanTime": "2024-01-15T17:00:00"
    }
  ],
  "skippedUserIds": ["user003"]
}
```

## 📋 Method Signatures

```csharp
// Process all users for a date
Task<WorkingHoursProcessingSummary> ProcessUserWorkingHoursAsync(DateTime date)

// Process specific user for a date
Task<decimal?> ProcessUserWorkingHoursForUserAsync(string userId, DateTime date)
```

## 🔗 API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/workingHours/process` | Process all users |
| POST | `/api/workingHours/process/{userId}` | Process one user |

## ⚙️ Configuration

The service is auto-registered in `Program.cs`:
```csharp
builder.Services.AddSingleton<IUserWorkingHoursService, UserWorkingHoursService>();
```

## 🧪 Testing

Use the provided API endpoints:
```bash
# Test: Process today
curl -X POST https://localhost:7xxx/api/workingHours/process

# Test: Process specific date
curl -X POST "https://localhost:7xxx/api/workingHours/process?date=2024-01-15"

# Test: Process specific user
curl -X POST https://localhost:7xxx/api/workingHours/process/user123
```

## 📈 Key Features

✅ **Batch Processing** - Process multiple users in one call
✅ **Minimum Record Check** - Requires 2+ records (ignores 1-record days)
✅ **Atomic Operations** - All-or-nothing processing per user
✅ **Comprehensive Logging** - Detailed operation logs
✅ **Error Handling** - Graceful error recovery
✅ **Stateless** - Thread-safe and scalable
✅ **RESTful** - Full REST API endpoints
✅ **Flexible** - Process by date or user or both

## 🔐 Data Integrity

- ✅ UNIQUE constraint on (UserId, LoginDate) in UserWorkingHours
- ✅ Transaction-like processing (all records marked processed together)
- ✅ Automatic upsert (INSERT OR REPLACE) if date already exists
- ✅ Decimal precision for working hours (not float)

## 📝 Implementation Notes

- **Minimum 2 Records Required** - Single sign-in/out days are ignored
- **Time Calculation** - `Hours = (LastTime - FirstTime).TotalHours`
- **Date Normalization** - All dates normalized to date-only (time removed)
- **Logging** - All operations logged at INFO, WARN, or ERROR level
- **Null Safety** - Proper null checks and error handling throughout

## 🎓 Next Steps

1. **Stop the debugger** - Restart the application (hot reload restart)
2. **Test the API** - Use provided curl commands
3. **Monitor Logs** - Check application logs for processing details
4. **Integrate** - Call the service from your business logic

## 📚 Documentation

- **Full Guide**: `WORKING_HOURS_CALCULATION_GUIDE.md`
- **Quick Reference**: `WORKING_HOURS_QUICKSTART.md`
- **This File**: `WORKING_HOURS_SUMMARY.md`
