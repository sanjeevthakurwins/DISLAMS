# DISLAMS Student Management System - Quick Start Guide

## 5-Minute Setup

### Prerequisites
- .NET SDK 8.0+ installed ([download](https://dotnet.microsoft.com/download))
- SQL Server or SQL Server LocalDB
- Git (optional, for version control)

### Step 1: Navigate to Project
```powershell
cd c:\Users\<YourUsername>\Downloads\DISLAMS\StudentManagementSystem
```

### Step 2: Update Database Connection (if needed)
Edit `DISLAMS.StudentManagement.API/appsettings.json`:

**If using LocalDB** (default):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DISLAMS_StudentManagement;Trusted_Connection=true;"
}
```

**If using SQL Server Express**:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=DISLAMS_StudentManagement;Trusted_Connection=true;"
}
```

**If using Azure SQL or remote server**:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=your-server.database.windows.net;Database=DISLAMS_StudentManagement;User Id=sa;Password=YourPassword;"
}
```

### Step 3: Build the Solution
```powershell
dotnet build
```

Expected output:
```
Build succeeded. 0 Error(s), 0 Warning(s)
```

If you see build errors, verify all project files exist and NuGet packages are installed.

### Step 4: Create Database & Migrations
```powershell
# Navigate to API project
cd DISLAMS.StudentManagement.API

# Create initial migration
dotnet ef migrations add InitialCreate --project ..\DISLAMS.StudentManagement.Infrastructure --startup-project .

# Apply migration (creates database)
dotnet ef database update --project ..\DISLAMS.StudentManagement.Infrastructure --startup-project .

# Return to root
cd ..
```

### Step 5: Run the API
```powershell
dotnet run --project DISLAMS.StudentManagement.API
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

### Step 6: Test in Swagger UI
Open browser: **https://localhost:5001/swagger/index.html**

You should see all 15+ endpoints documented.

---

## Test a Complete Workflow

### Using Swagger UI (Easiest)

#### 1. Create Attendance (Draft)
```
POST /api/attendance/create

Request Body:
{
  "studentId": "12345678-1234-1234-1234-123456789012",
  "courseId": "87654321-4321-4321-4321-210987654321",
  "attendanceDate": "2026-01-28",
  "isPresent": true,
  "remarks": "On time",
  "actorId": "11111111-1111-1111-1111-111111111111",
  "actorRole": "Teacher"
}

Response: 201 Created
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "status": "Draft",
  "isPresent": true,
  ...
}
```

**⭐ Copy the returned `id` value - you'll need it for next steps**

#### 2. Submit Attendance (Draft → Submitted)
```
POST /api/attendance/{id}/submit

Request Body:
{
  "actorId": "11111111-1111-1111-1111-111111111111",
  "actorRole": "Teacher"
}

Response: 200 OK
```

#### 3. Approve Attendance (Submitted → Approved)
```
POST /api/attendance/{id}/approve

Request Body:
{
  "actorId": "22222222-2222-2222-2222-222222222222",
  "actorRole": "AcademicCoordinator",
  "approvalNotes": "Verified and approved"
}

Response: 200 OK
```

#### 4. Publish Attendance (Approved → Published)
```
POST /api/attendance/{id}/publish

Request Body:
{
  "actorId": "22222222-2222-2222-2222-222222222222",
  "actorRole": "AcademicCoordinator"
}

Response: 200 OK
```

#### 5. View Audit Trail
```
GET /api/attendance/{id}/audit-trail

Response: 200 OK
[
  {
    "id": "...",
    "action": "Created",
    "newStatus": "Draft",
    "actorName": "Actor 1",
    "actorRole": "Teacher",
    "actionTimestamp": "2026-01-28T10:00:00Z"
  },
  {
    "action": "Submitted",
    "previousStatus": "Draft",
    "newStatus": "Submitted",
    "actionTimestamp": "2026-01-28T11:00:00Z"
  },
  ...
]
```

---

## Troubleshooting

### Problem: Build Fails with "Project not found"

**Solution:**
```powershell
# Verify you're in the right directory
cd c:\Users\ditsd\Downloads\DISLAMS\StudentManagementSystem

# Check solution file exists
dir *.sln

# Try cleaning and rebuilding
dotnet clean
dotnet restore
dotnet build
```

### Problem: Database Connection Error

**Symptom:** 
```
A network-related or instance-specific error occurred...
```

**Solutions:**

1. **If using LocalDB**, ensure it's installed:
```powershell
# Check if LocalDB is installed
sqllocaldb info
```

2. **Update connection string** in `appsettings.json`

3. **Try SQLite instead** (for development):
   - Edit `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=dislams.db;"
   }
   ```
   - Edit `Program.cs` and replace UseSqlServer with UseSqlite:
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```
   - Install SQLite package:
   ```powershell
   dotnet add DISLAMS.StudentManagement.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
   ```

### Problem: "dotnet: command not found"

**Solution:**
- Verify .NET 8 SDK is installed: `dotnet --version`
- If not installed, [download from microsoft.com](https://dotnet.microsoft.com/download)
- Restart PowerShell after installation

### Problem: Swagger not showing after running

**Solution:**
- Ensure you're accessing HTTPS: `https://localhost:5001/swagger`
- Not HTTP: `http://localhost:5001/swagger` (won't work)
- If still blank, check console output for startup errors

### Problem: "Migration has not been applied"

**Solution:**
```powershell
# Remove previous migration
dotnet ef migrations remove --project DISLAMS.StudentManagement.Infrastructure --startup-project DISLAMS.StudentManagement.API

# Try again
dotnet ef migrations add InitialCreate --project DISLAMS.StudentManagement.Infrastructure --startup-project DISLAMS.StudentManagement.API
dotnet ef database update --project DISLAMS.StudentManagement.Infrastructure --startup-project DISLAMS.StudentManagement.API
```

---

## Project Structure at a Glance

```
StudentManagementSystem/
├── DISLAMS.StudentManagement.Domain/          ← Business rules & entities
├── DISLAMS.StudentManagement.Application/     ← Commands, Queries, Handlers
├── DISLAMS.StudentManagement.Infrastructure/  ← Database & Repositories
├── DISLAMS.StudentManagement.API/             ← REST API Endpoints
├── DISLAMS.StudentManagement.sln              ← Solution file
├── README.md                                   ← Full documentation
└── ARCHITECTURE.md                             ← Architecture decisions
```

---

## API Endpoints Quick Reference

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/attendance/create` | Create attendance (Draft) |
| POST | `/api/attendance/{id}/submit` | Submit for approval |
| POST | `/api/attendance/{id}/approve` | Approve attendance |
| POST | `/api/attendance/{id}/publish` | Publish to permanent record |
| POST | `/api/attendance/{id}/lock` | Finalize attendance |
| POST | `/api/attendance/{id}/request-reopen` | Request to edit |
| POST | `/api/attendance/reopen-request/{id}/approve` | Approve reopen request |
| POST | `/api/attendance/{id}/apply-correction` | Create corrected version |
| GET | `/api/attendance/{id}` | Get single record |
| GET | `/api/attendance/student/{id}/date/{date}/course/{id}` | Get specific attendance |
| GET | `/api/attendance/student/{id}/range` | Get date range for student |
| GET | `/api/attendance/course/{id}/date/{date}` | Get class attendance |
| GET | `/api/attendance/status/{status}` | Get by status |
| GET | `/api/attendance/versions/...` | Get all versions |
| GET | `/api/attendance/{id}/audit-trail` | Get change history |

---

## Understanding the State Machine

```
NORMAL WORKFLOW:
┌──────┐     ┌─────────┐      ┌──────────┐     ┌───────────┐      ┌────────┐
│Draft │────▶│Submitted│─────▶│ Approved │────▶│ Published │─────▶│ Locked │
└──────┘     └─────────┘      └──────────┘     └───────────┘      └────────┘

CORRECTION WORKFLOW:
┌──────────┐     ┌──────────────────┐
│ Published│────▶│ Corrected        │──────┐
└──────────┘     │ (original marked)│      │
                 └──────────────────┘      │
                                           ▼
                                      ┌──────┐
                                      │ New  │
                                      │Draft │
                                      └──────┘

REOPEN WORKFLOW:
┌──────────┐ or ┌──────────┐    ┌──────────────────┐     ┌──────┐
│Submitted │    │ Approved │───▶│ ReopenRequested  │────▶│Draft │
└──────────┘    └──────────┘    └──────────────────┘     └──────┘
```

**Key Rules:**
- ✅ Can only submit Draft records (within 24 hours of creation)
- ✅ Only AcademicCoordinator can approve/publish
- ✅ Published records cannot be edited directly - must create correction
- ✅ Corrections create NEW versions, original marked as "Corrected"
- ✅ Every state change is logged in audit trail

---

## Common Tasks

### Task 1: Mark All Students Present for a Class

```powershell
# Pseudo-code - implement in your client

foreach student in class:
    1. POST /api/attendance/create
       - studentId: student.Id
       - courseId: class.CourseId
       - isPresent: true
       - actorRole: "Teacher"
       
    2. POST /api/attendance/{id}/submit
       - actorRole: "Teacher"
```

### Task 2: Get Attendance Report for a Student

```
GET /api/attendance/student/12345678-1234-1234-1234-123456789012/range?startDate=2026-01-01&endDate=2026-01-31

Returns: All attendance records for the month
```

### Task 3: Correct a Published Attendance Record

```
1. POST /api/attendance/{id}/apply-correction
   - isPresent: false  (corrected value)
   - correctionReason: "Data entry error"
   - actorRole: "AcademicCoordinator"

Result: 
  - Original record marked as "Corrected"
  - New version created in Draft state
  - New version goes through approval workflow
```

### Task 4: Find What Changed

```
GET /api/attendance/{id}/audit-trail

Shows: Complete history of who did what and when
  - Created
  - Submitted
  - Approved
  - Published
  - Locked
  - (or) Corrected
```

---

## Development Tips

### Enable Detailed Logging

Edit `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

### Run with Watch Mode

```powershell
dotnet watch run --project DISLAMS.StudentManagement.API
```

This automatically rebuilds and restarts when you change code.

### Use Visual Studio or VS Code

**Visual Studio:**
1. Open DISLAMS.StudentManagement.sln
2. Set DISLAMS.StudentManagement.API as startup project
3. Press F5 to run

**VS Code:**
1. Open folder containing solution
2. Press Ctrl+Shift+D (Debug view)
3. Click "Run and Debug"

### Test with Postman

1. Open Postman
2. Import Swagger: `https://localhost:5001/swagger/v1/swagger.json`
3. Postman auto-generates all endpoints with examples
4. Test workflows using the endpoint collection

---

## Next Steps

1. **Understand the Architecture** → Read [ARCHITECTURE.md](ARCHITECTURE.md)
2. **Review Full API Docs** → Read [README.md](README.md)
3. **Test the Workflow** → Follow "Test a Complete Workflow" above
4. **Explore the Code** → Browse the solution structure
5. **Add Unit Tests** → Create test project for your changes
6. **Deploy** → Build release and deploy to production

---

## Support

For detailed information:
- **Architecture decisions** → See [ARCHITECTURE.md](ARCHITECTURE.md)
- **API documentation** → See [README.md](README.md) or Swagger UI
- **Build/run issues** → See troubleshooting section above
- **Code questions** → Review the Domain entities and Command handlers

---

**Happy coding! The system is now ready to manage attendance with complete governance and audit trails.**
