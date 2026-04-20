# 🚀 START HERE - Working Hours Service

Welcome! You have a new **Working Hours Calculation Service** ready to use.

---

## ⏱️ 30-SECOND OVERVIEW

This service automatically calculates how many hours users worked based on attendance records (scan times).

**Input:** Multiple attendance scans  
**Process:** Calculate duration  
**Output:** Working hours stored in database  

---

## 🎯 CHOOSE YOUR PATH

### Path 1: "Just Show Me How to Use It" (5 minutes)
1. Read: **WORKING_HOURS_QUICKSTART.md**
2. Run: Provided curl commands
3. Done! ✅

### Path 2: "I Need to Integrate It" (30 minutes)
1. Read: **WORKING_HOURS_QUICKSTART.md** (5 min)
2. Read: **CODE_STRUCTURE_OVERVIEW.md** (15 min)
3. Write: Integration code (10 min)
4. Done! ✅

### Path 3: "Tell Me Everything" (1 hour)
1. Read: **FINAL_SUMMARY.md** (10 min)
2. Read: **WORKING_HOURS_CALCULATION_GUIDE.md** (30 min)
3. Review: **VISUAL_GUIDE.md** (20 min)
4. Done! ✅

---

## 🔥 QUICK START (Copy & Paste)

### Test It Now
```bash
curl -X POST https://localhost:7xxx/api/workingHours/process
```

Replace `7xxx` with your actual port number.

### Use in C#
```csharp
// Inject the service
private readonly IUserWorkingHoursService _workingHours;

// Calculate working hours
var summary = await _workingHours.ProcessUserWorkingHoursAsync(DateTime.Today);
Console.WriteLine($"Processed: {summary.TotalUsersProcessed} users");
```

---

## 📚 DOCUMENTATION ROADMAP

```
START HERE (you are here)
    │
    ├─ 5 min version?
    │   → WORKING_HOURS_QUICKSTART.md
    │
    ├─ Need to integrate?
    │   → CODE_STRUCTURE_OVERVIEW.md
    │
    ├─ Want diagrams?
    │   → VISUAL_GUIDE.md
    │
    ├─ Need everything?
    │   → WORKING_HOURS_CALCULATION_GUIDE.md
    │
    └─ Lost?
        → DOCUMENTATION_INDEX.md
```

---

## 🎯 What This Service Does

```
┌────────────────────┐
│ Attendance Records │  Two people scanned in/out
│ (Login/Logout)     │  
└────────┬───────────┘
         │
         ▼
   [Service runs]
    Calculates hours
         │
         ▼
┌────────────────────┐
│ Working Hours Table│  "John worked 8.5 hours"
│ (Results saved)    │  "Jane worked 8.0 hours"
└────────────────────┘
```

---

## ✨ Key Features

✅ **Automatic** - No manual entry
✅ **Fast** - < 1 second per date
✅ **Easy** - Simple API
✅ **Safe** - Data validation
✅ **Complete** - Fully documented

---

## 🧪 Test It in 3 Steps

### Step 1: Open Terminal
```bash
# Linux/Mac/PowerShell
```

### Step 2: Run This Command
```bash
curl -X POST https://localhost:7xxx/api/workingHours/process
```

### Step 3: Check Response
```json
{
  "processedDate": "2024-01-15T00:00:00",
  "totalUsersProcessed": 5,
  "usersSkipped": 2,
  "processedRecords": [...],
  "skippedUserIds": [...]
}
```

**If you see this, it works! ✅**

---

## 📋 What You Need to Know

### Minimum Records
- Need **2+ attendance scans** per user per day
- If only 1 scan → User is skipped
- This is by design (prevents false hours)

### Calculation
```
Hours = Last Scan Time - First Scan Time

Example:
  In:  09:00
  Out: 17:30
  ────────────
  Hours: 8.5
```

### Data Stored
- **UserId** - Who
- **LoginDate** - When
- **WorkingHours** - How many hours (as decimal)

---

## 🔧 How to Use

### Via REST API (Easiest)
```bash
# Process all users for today
curl -X POST https://localhost:7xxx/api/workingHours/process

# Process specific date
curl -X POST "https://localhost:7xxx/api/workingHours/process?date=2024-01-15"

# Process one user
curl -X POST https://localhost:7xxx/api/workingHours/process/user123
```

### Via C# (Most Powerful)
```csharp
// Inject service into your class
public class MyService {
    private readonly IUserWorkingHoursService _workingHours;
    
    public MyService(IUserWorkingHoursService workingHours) {
        _workingHours = workingHours;
    }
    
    public async Task CalculateHours() {
        // Process today
        var result = await _workingHours
            .ProcessUserWorkingHoursAsync(DateTime.Today);
        
        Console.WriteLine($"Processed: {result.TotalUsersProcessed}");
        Console.WriteLine($"Skipped: {result.UsersSkipped}");
    }
}
```

---

## ❓ Common Questions

### Q: How do I run this?
**A:** The service runs automatically when you call it. Via API or C# code.

### Q: Where is the data stored?
**A:** In the UserWorkingHours table (SQLite database, same as attendance).

### Q: What if something goes wrong?
**A:** Check the logs. All operations are logged with details.

### Q: Can I run it daily?
**A:** Yes! Add it to a background job to run every night.

### Q: What if a user has 1 scan?
**A:** They're skipped (added to skippedUserIds list).

### Q: How long does it take?
**A:** < 1 second for typical daily processing.

---

## 📖 Documentation Files

| File | What It Is | Read Time |
|------|-----------|-----------|
| **QUICKSTART** | Simple guide | 5 min |
| **GUIDE** | Full technical docs | 20 min |
| **VISUAL** | Diagrams & flows | 15 min |
| **CODE** | Architecture | 15 min |
| **README** | Overview | 10 min |
| **INDEX** | Navigation | 2 min |

---

## 🎯 Your Next Step

Pick one:

1. **I want to test it now**
   → Run: `curl -X POST https://localhost:7xxx/api/workingHours/process`

2. **I want to understand it**
   → Read: **WORKING_HOURS_QUICKSTART.md**

3. **I want to integrate it**
   → Read: **CODE_STRUCTURE_OVERVIEW.md**

4. **I want everything**
   → Read: **WORKING_HOURS_CALCULATION_GUIDE.md**

5. **I'm lost**
   → Read: **DOCUMENTATION_INDEX.md**

---

## ✅ Checklist

Before you start:
- [ ] FaceRecognition.Api is running
- [ ] You have attendance records in database
- [ ] You know your port number (7xxx)
- [ ] You have curl or HTTP client

---

## 🎉 You're Ready!

Everything is set up and ready to use.

**Start with:** WORKING_HOURS_QUICKSTART.md

**Questions?** Check DOCUMENTATION_INDEX.md for links to all docs.

---

**Happy coding! 🚀**
