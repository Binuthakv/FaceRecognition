# Attendance Records Web Page

## Overview
A new web page has been created in the FaceRecognition.Api project to display attendance records in a table format.

## Features

### 📋 Attendance List Page
**Location:** `FaceRecognition.Api/Pages/AttendanceList.cshtml`

The attendance records page displays:
- **User ID** - Unique identifier for each user
- **Name** - Full name of the user (fetched from the user database)
- **Scan Time** - Date and time when the attendance was recorded
- **Status** - Processing status (Processed/Pending)

### ✨ Additional Features
- **Statistics Dashboard** - Shows total records, processed count, and unprocessed count
- **Responsive Design** - Works on desktop, tablet, and mobile devices
- **Light/Dark Theme Support** - Adapts to system theme
- **Refresh Functionality** - Manual refresh button to reload latest records
- **Empty State** - Friendly message when no records are available
- **Error Handling** - Displays error messages if data loading fails

## Accessing the Page

### Running the API
1. Start the FaceRecognition.Api project
2. Navigate to the home page: `https://localhost:7xxx/` (replace xxx with your port)
3. Click on "📋 Attendance Records" card
4. Or directly navigate to: `https://localhost:7xxx/AttendanceList`

### API Endpoints
The page pulls data from the existing attendance endpoints:
- `GET /api/attendance` - Retrieves all attendance records
- Joins data with `/api/users` to fetch user names

## Files Created/Modified

### New Files:
1. **FaceRecognition.Api/Pages/AttendanceList.cshtml.cs**
   - PageModel that handles data fetching
   - Combines attendance records with user information
   - Formats display data

2. **FaceRecognition.Api/Pages/AttendanceList.cshtml**
   - Razor view with styled HTML table
   - Responsive design with statistics
   - Error and status message handling

3. **FaceRecognition.Api/Pages/Index.cshtml**
   - Home page with navigation to key features
   - Links to API documentation (Swagger/Scalar)

### Modified Files:
1. **FaceRecognition.Api/Program.cs**
   - Added `AddRazorPages()` service
   - Added `MapRazorPages()` endpoint mapping

2. **FaceRecognitionApp/AppShell.xaml**
   - Added Attendance tab to navigation (MAUI app)
   - Integrated with existing tab navigation

## Technical Details

### Technology Stack
- **ASP.NET Core Razor Pages** (.NET 10)
- **C# 13** with LINQ
- **HTML/CSS** for responsive UI
- **MVC Pattern** with PageModel

### Design
- **Color Scheme:** Purple gradient theme (#667eea to #764ba2)
- **Styling:** Custom CSS with responsive grid layout
- **Table Display:** Styled with hover effects and visual hierarchy
- **Status Badges:** Color-coded (Green for processed, Red for pending)

### Data Flow
1. Page loads → OnGetAsync method executes
2. Fetches attendance records from IAttendanceService
3. Fetches user information from IUserDatabaseService
4. Merges data (UserId + Name)
5. Sorts by ScanTime (newest first)
6. Renders in table with formatting

## Usage Example

### Viewing Statistics
The page automatically displays:
- Total number of attendance records
- Count of processed records
- Count of pending/unprocessed records

### Filtering Unprocessed Records
Users can navigate to `/api/attendance/unprocessed` API endpoint directly if they need to work with unprocessed records only.

## Styling Features

### Table Design
- Sticky header with clear visual hierarchy
- Hover effects for better interactivity
- Alternating row backgrounds for readability
- Responsive breakpoints for mobile devices

### Status Indicators
- ✓ Processed - Green badge
- ⏳ Pending - Red badge

## Future Enhancements (Optional)

Consider adding:
1. Sorting by columns
2. Pagination for large datasets
3. Date range filtering
4. Search/filter by user ID or name
5. Export to CSV/PDF
6. Real-time updates with WebSockets
