## 🎯 Attendance Records Web Page - Quick Start

### Access the Page
**URL:** `https://localhost:PORT/AttendanceList`

or navigate from the home page: `https://localhost:PORT/`

---

## 📊 What You'll See

### Table Columns
| Column | Description |
|--------|-------------|
| **User ID** | Unique identifier (formatted as code) |
| **Name** | Full name of registered user |
| **Scan Time** | Date & time of attendance record |
| **Status** | ✓ Processed or ⏳ Pending |

### Statistics Cards (Top of Page)
- **Total Records** - All attendance entries
- **Processed** - Records that have been processed
- **Unprocessed** - Records pending processing

---

## 🛠️ Files Created

**In FaceRecognition.Api Project:**
```
FaceRecognition.Api/
├── Pages/
│   ├── AttendanceList.cshtml          (UI - Table view)
│   ├── AttendanceList.cshtml.cs       (PageModel - Logic)
│   └── Index.cshtml                   (Home page)
```

**In FaceRecognitionApp (MAUI):**
- Added "Attendance" tab to navigation with 📋 icon

---

## 📱 Features

✅ **Responsive Design** - Works on all screen sizes
✅ **Real-time Data** - Fetches current attendance records
✅ **User Information** - Displays user names with IDs
✅ **Status Indicators** - Color-coded processing status
✅ **Statistics** - Quick overview of records
✅ **Refresh Button** - Reload latest data
✅ **Error Handling** - Graceful error messages
✅ **Sorting** - Newest records appear first

---

## 🔧 Technical Stack

- **Framework:** ASP.NET Core Razor Pages (.NET 10)
- **Data Access:** IAttendanceService + IUserDatabaseService
- **Styling:** Responsive CSS with gradient theme
- **Architecture:** MVC Pattern with PageModel

---

## 📡 API Integration

The page uses these existing endpoints:
- `GET /api/attendance` - All records
- `GET /api/users` - User information (for name lookup)

---

## 🎨 Design Highlights

- **Color Theme:** Purple gradient (#667eea → #764ba2)
- **Hover Effects:** Interactive table rows
- **Status Badges:** Green (Processed) / Red (Pending)
- **Empty State:** Friendly message when no records exist
- **Mobile Responsive:** Adapts to screens ≤768px

---

## 🚀 To Run

1. **Start the API:**
   ```
   dotnet run --project FaceRecognition.Api
   ```

2. **Open Browser:**
   ```
   https://localhost:7xxx/AttendanceList
   ```

3. **Or visit home page:**
   ```
   https://localhost:7xxx/
   ```

---

## 💡 Tips

- Click **Refresh** button to reload latest records
- Records are sorted with newest first
- Hover over table rows for visual feedback
- Status badges color-code processing state
- Page handles empty results gracefully

---

**Created:** $(date)
**Project:** Face Recognition System
**Version:** .NET 10
