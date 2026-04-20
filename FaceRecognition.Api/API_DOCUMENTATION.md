# Working Hours API Endpoints Documentation

## Overview

The Working Hours API provides comprehensive endpoints for processing attendance records and calculating working hours statistics at various time intervals (daily, weekly, and monthly). All filtering parameters support date ranges and user-specific queries.

---

## Base URL
```
/api/workinghours
```

---

## Endpoints

### 1. Process Working Hours (Daily)

**Endpoint:** `POST /api/workinghours/process`

**Description:** Process attendance records for a specific date and calculate working hours.

**Parameters:**
- `date` (optional, query): Date to process (format: `yyyy-MM-dd`). Defaults to today.

**Example Request:**
```
POST /api/workinghours/process?date=2024-01-15
```

**Response:**
```json
{
  "processedDate": "2024-01-15",
  "totalUsersProcessed": 5,
  "usersSkipped": 2,
  "processedRecords": [
    {
      "userId": "user001",
      "workingHours": 8.5,
      "recordsProcessed": 2,
      "firstScanTime": "2024-01-15T08:00:00",
      "lastScanTime": "2024-01-15T16:30:00"
    }
  ],
  "skippedUserIds": ["user002", "user003"]
}
```

---

### 2. Process Working Hours for Specific User

**Endpoint:** `POST /api/workinghours/process/{userId}`

**Description:** Process working hours for a specific user on a specific date.

**Parameters:**
- `userId` (required, route): User ID to process
- `date` (optional, query): Date to process (format: `yyyy-MM-dd`). Defaults to today.

**Example Request:**
```
POST /api/workinghours/process/user001?date=2024-01-15
```

**Response:**
```json
{
  "userId": "user001",
  "date": "2024-01-15",
  "workingHours": 8.5
}
```

**Error Response (insufficient records):**
```json
{
  "error": "Insufficient attendance records (minimum 2 required) for the specified user and date"
}
```

---

### 3. Get Working Hours Summary with Filters

**Endpoint:** `GET /api/workinghours/summary`

**Description:** Get working hours records with optional filtering by date range and/or user ID.

**Query Parameters:**
- `startDate` (optional, query): Start date for filtering (format: `yyyy-MM-dd`)
- `endDate` (optional, query): End date for filtering (format: `yyyy-MM-dd`)
- `userId` (optional, query): Filter by specific user ID

**Example Requests:**
```
GET /api/workinghours/summary
GET /api/workinghours/summary?startDate=2024-01-01&endDate=2024-01-31
GET /api/workinghours/summary?userId=user001
GET /api/workinghours/summary?startDate=2024-01-01&endDate=2024-01-31&userId=user001
```

**Response:**
```json
{
  "records": [
    {
      "id": 1,
      "userId": "user001",
      "loginDate": "2024-01-15",
      "workingHours": 8.5
    },
    {
      "id": 2,
      "userId": "user001",
      "loginDate": "2024-01-16",
      "workingHours": 9.0
    }
  ],
  "totalHours": 17.5,
  "averageHours": 8.75,
  "uniqueUsers": 1,
  "recordsCount": 2
}
```

---

### 4. Get Daily Working Hours Summary

**Endpoint:** `GET /api/workinghours/daily`

**Description:** Get daily working hours summary for all users within a date range.

**Query Parameters:**
- `startDate` (required, query): Start date (format: `yyyy-MM-dd`)
- `endDate` (required, query): End date (format: `yyyy-MM-dd`)
- `userId` (optional, query): Filter by specific user ID

**Example Requests:**
```
GET /api/workinghours/daily?startDate=2024-01-01&endDate=2024-01-31
GET /api/workinghours/daily?startDate=2024-01-01&endDate=2024-01-31&userId=user001
```

**Response:**
```json
[
  {
    "date": "2024-01-15",
    "userId": "user001",
    "workingHours": 8.5
  },
  {
    "date": "2024-01-15",
    "userId": "user002",
    "workingHours": 9.0
  },
  {
    "date": "2024-01-16",
    "userId": "user001",
    "workingHours": 8.75
  }
]
```

---

### 5. Get Weekly Working Hours Summary

**Endpoint:** `GET /api/workinghours/weekly`

**Description:** Get weekly working hours summary for all users within a date range.

**Query Parameters:**
- `startDate` (required, query): Start date (format: `yyyy-MM-dd`)
- `endDate` (required, query): End date (format: `yyyy-MM-dd`)
- `userId` (optional, query): Filter by specific user ID

**Example Requests:**
```
GET /api/workinghours/weekly?startDate=2024-01-01&endDate=2024-02-29
GET /api/workinghours/weekly?startDate=2024-01-01&endDate=2024-02-29&userId=user001
```

**Response:**
```json
[
  {
    "year": 2024,
    "week": 2,
    "weekStart": "2024-01-08",
    "weekEnd": "2024-01-14",
    "userId": "user001",
    "totalHours": 40.5,
    "daysWorked": 5,
    "averageHoursPerDay": 8.1
  },
  {
    "year": 2024,
    "week": 3,
    "weekStart": "2024-01-15",
    "weekEnd": "2024-01-21",
    "userId": "user001",
    "totalHours": 42.0,
    "daysWorked": 5,
    "averageHoursPerDay": 8.4
  }
]
```

---

### 6. Get Monthly Working Hours Summary

**Endpoint:** `GET /api/workinghours/monthly`

**Description:** Get monthly working hours summary for all users within a date range.

**Query Parameters:**
- `startDate` (required, query): Start date (format: `yyyy-MM-dd`)
- `endDate` (required, query): End date (format: `yyyy-MM-dd`)
- `userId` (optional, query): Filter by specific user ID

**Example Requests:**
```
GET /api/workinghours/monthly?startDate=2024-01-01&endDate=2024-12-31
GET /api/workinghours/monthly?startDate=2024-01-01&endDate=2024-12-31&userId=user001
```

**Response:**
```json
[
  {
    "year": 2024,
    "month": 1,
    "monthName": "January",
    "userId": "user001",
    "totalHours": 168.0,
    "daysWorked": 21,
    "averageHoursPerDay": 8.0
  },
  {
    "year": 2024,
    "month": 2,
    "monthName": "February",
    "userId": "user001",
    "totalHours": 160.0,
    "daysWorked": 20,
    "averageHoursPerDay": 8.0
  }
]
```

---

## HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 400 | Bad Request (invalid parameters, missing required fields) |
| 500 | Internal Server Error |

---

## Error Responses

**Invalid Date Range:**
```json
{
  "error": "startDate cannot be greater than endDate"
}
```

**Missing Required Parameters:**
```json
{
  "error": "startDate and endDate are required"
}
```

**Insufficient Records:**
```json
{
  "error": "Insufficient attendance records (minimum 2 required) for the specified user and date"
}
```

---

## Usage Examples

### Example 1: Get monthly summary for a specific user in 2024

```bash
curl -X GET "https://api.example.com/api/workinghours/monthly?startDate=2024-01-01&endDate=2024-12-31&userId=user001"
```

### Example 2: Get weekly summary for all users in January

```bash
curl -X GET "https://api.example.com/api/workinghours/weekly?startDate=2024-01-01&endDate=2024-01-31"
```

### Example 3: Process working hours for today

```bash
curl -X POST "https://api.example.com/api/workinghours/process"
```

### Example 4: Get working hours summary with filters

```bash
curl -X GET "https://api.example.com/api/workinghours/summary?startDate=2024-01-15&endDate=2024-01-31&userId=user001"
```

---

## Notes

- All dates should be provided in ISO 8601 format: `yyyy-MM-dd`
- Working hours are calculated as the difference between the last and first attendance scan time for a given period
- Minimum of 2 attendance records are required to calculate working hours for a day
- Week numbers follow the ISO 8601 standard
- Months are represented as numbers (1-12) and include localized month names
- All times are returned in UTC format
- The API performs all calculations server-side for accuracy and consistency
