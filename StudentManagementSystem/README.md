# DISLAMS Student Management System - API Documentation

## Project Overview

**DISLAMS** (Distributed Integrated School Learning and Management System) - **Phase 2: Governed Attendance & Student Management Service**

This is a **governance-first**, **backend-only** ASP.NET Core 8 implementation of an attendance management system built with strict system-thinking principles, following DISLAMS Phase-2 Technical Assignment requirements.

### Core Philosophy

> "Who can build systems that cannot be misused."

This system is NOT about "Who knows .NET Core best" - it's about building systems with:
- **Governance Discipline** - Every action is controlled and audited
- **Data Integrity** - Historical immutability and versioning  
- **System Thinking** - No overwrite of records, no silent edits
- **Clear Reasoning** - Every decision defensible and explainable

---

## Project Architecture

### Solution Structure

```
DISLAMS.StudentManagement/
├── DISLAMS.StudentManagement.Domain/          (Domain Layer - Pure Business Logic)
│   ├── Entities/
│   │   ├── Entity.cs                          (Base entity with audit info)
│   │   ├── Student.cs                         (Student entity)
│   │   ├── Course.cs                          (Course/Subject entity)
│   │   ├── AttendanceRecord.cs                (Core attendance record with versioning)
│   │   ├── AuditLog.cs                        (Immutable audit trail)
│   │   ├── AttendanceException.cs             (Exception/special cases)
│   │   ├── ReopenRequest.cs                   (Reopen request workflow)
│   │   └── Actor.cs                           (User/Role mapping)
│   ├── Enums/
│   │   ├── AttendanceStatus.cs                (State machine states)
│   │   ├── UserRole.cs                        (Teacher, AcademicCoordinator, Leadership)
│   │   └── ExceptionType.cs                   (Exception types)
│   └── Repositories/                          (Repository interfaces)
│       ├── IRepository.cs                     (Generic interface)
│       ├── IAttendanceRepository.cs           (Specialized attendance queries)
│       └── IAuditLogRepository.cs             (Audit log queries)
│
├── DISLAMS.StudentManagement.Application/     (Application Layer - CQRS & Use Cases)
│   ├── Commands/
│   │   └── AttendanceCommands.cs              (CQRS Commands:Create, Submit, Approve, Publish, etc.)
│   ├── Queries/
│   │   └── AttendanceQueries.cs               (CQRS Queries: Get, GetRange, GetByStatus, etc.)
│   ├── Handlers/
│   │   ├── AttendanceCommandHandlers.cs       (Command handlers with business logic)
│   │   ├── MoreAttendanceCommandHandlers.cs   (Reopen, Correction, Lock handlers)
│   │   └── QueryHandlers.cs                   (Query handlers)
│   ├── DTOs/
│   │   └── ApplicationDtos.cs                 (AttendanceDto, StudentDto, CourseDto, AuditLogDto)
│   └── Mapping/
│       └── MappingProfile.cs                  (AutoMapper configurations)
│
├── DISLAMS.StudentManagement.Infrastructure/  (Infrastructure Layer - Data Access & EF Core)
│   ├── Data/
│   │   └── ApplicationDbContext.cs            (EF Core DbContext with all entity configurations)
│   └── Repositories/
│       ├── Repository.cs                      (Generic repository implementation)
│       ├── AttendanceRepository.cs            (Specialized attendance repository)
│       └── AuditLogRepository.cs              (Append-only audit log repository)
│
├── DISLAMS.StudentManagement.API/             (Presentation Layer - REST API)
│   ├── Controllers/
│   │   └── AttendanceController.cs            (All attendance endpoints with Swagger docs)
│   ├── Program.cs                             (Service configuration & middleware setup)
│   └── appsettings.json                       (Configuration)
│
└── DISLAMS.StudentManagement.sln              (Solution file)
```

---

## Key Design Decisions & Rationale

### 1. **State Machine Architecture**

**Problem:** Attendance can be edited at any point, leading to data integrity issues.

**Solution:**
```
Draft → Submitted → Approved → Published → Locked
                                    ↓
                           ReopenRequested ← (can request)
                                    ↓
                              Draft (reopened)
                                    ↓
                              (new version after correction)
```

**Why?**
- Enforces valid state transitions (cannot jump states)
- Role-based authorization at each state
- Each state has specific allowed actions
- Published records can only be corrected (new version created)
- Locked records are final

---

### 2. **Versioning Instead of Overwriting**

**Problem:** Teachers might edit submitted attendance, losing audit trail.

**Solution:**
- Attendance records are immutable once submitted
- Corrections create NEW versions, not edits
- Original record marked as "Corrected"  
- Version number incremented
- Complete history preserved

**Data Model:**
```csharp
AttendanceRecord {
    int Version,           // 0 = original, 1+ = corrections
    Guid? ParentVersionId, // Points to previous version
    AttendanceStatus Status // Tracks lifecycle
}
```

**Why?**
- No silent edits
- Complete audit trail
- Governance compliance
- Non-repudiation (user cannot deny action)

---

### 3. **Immutable Audit Logs**

**Problem:** Audit logs can be tampered with or deleted.

**Solution:**
```csharp
public class AuditLog : Entity
{
    // Cannot be updated
    // Cannot be deleted
    // Append-only pattern
}

public class AuditLogRepository : IRepository
{
    public override async Task<AuditLog> UpdateAsync(AuditLog entity)
    {
        throw new InvalidOperationException("Audit logs are immutable and cannot be updated.");
    }
    
    public override async Task DeleteAsync(Guid id)
    {
        throw new InvalidOperationException("Audit logs are immutable and cannot be deleted.");
    }
}
```

**Every action logs:**
- WHO (Actor ID & Role)
- WHAT (Action name)
- WHEN (Timestamp)
- BEFORE → AFTER (State transition)
- WHY (Reason/remarks)

**Why?**
- Non-repudiation
- Regulatory compliance
- Complete audit trail
- Governance assurance

---

### 4. **Role-Based Authorization at Action Level**

**Problem:** Anyone could approve/publish attendance.

**Solution:**
- Each command validates role upfront
- Each state transition checked for role permission

**Roles:**
- **Teacher**: Create, Submit, Request Reopen
- **AcademicCoordinator**: Approve, Publish, Apply Corrections, Approve Reopens, Lock
- **Leadership**: View/Report only

**Example:**
```csharp
public async Task<AttendanceDto> Handle(ApproveAttendanceCommand request, ...)
{
    // Only Academic Coordinators can approve
    if (request.ActorRole != "AcademicCoordinator")
        throw new UnauthorizedAccessException("Only Academic Coordinators can approve attendance.");
    
    // Validate state transition
    if (attendance.Status != AttendanceStatus.Submitted)
        throw new InvalidOperationException("Can only approve Submitted attendance...");
        
    // Proceed with approval...
}
```

**Why?**
- Prevents unauthorized actions
- Clear accountability
- Auditability at every level

---

### 5. **Exception Handling as First-Class**

**Problem:** System failures, late submissions, parent disputes not tracked.

**Solution:**
Model exceptions explicitly:
```csharp
public enum ExceptionType
{
    TeacherAbsent = 1,      // Teacher was absent
    LateSubmission = 2,     // Submitted after deadline
    ParentDispute = 3,      // Parent disputes attendance
    SystemFailure = 4,      // System failure prevented marking
    Other = 5
}
```

**Why?**
- Governance requirement
- Regulatory compliance
- Audit trail of unusual circumstances
- Fair process for disputes

---

### 6. **Code-First EF Core with Migrations**

**Problem:** Manual SQL doesn't version with code changes.

**Solution:**
```bash
dotnet ef migrations add InitialCreate --project DISLAMS.StudentManagement.Infrastructure --startup-project DISLAMS.StudentManagement.API
dotnet ef database update
```

**Advantages:**
- Database schema in code (version control)
- Easy to rollback/forward
- IDE support for navigation
- Compile-time verification

---

### 7. **CQRS Pattern with MediatR**

**Problem:** Complex commands and queries mixed in one handler.

**Solution:**
- **Commands** - State-changing operations (Create, Submit, Approve, etc.)
- **Queries** - Read-only operations (Get, List, GetAuditTrail, etc.)
- **Handlers** - Separate classes for each command/query
- **MediatR** - Dispatches to correct handler

**Why?**
- Single Responsibility Principle
- Easier to test
- Clear separation of concerns
- Scalable as system grows
- Natural event sourcing foundation

---

### 8. **AutoMapper for DTO Transformation**

**Problem:** Manual mapping between entities and DTOs is error-prone.

**Solution:**
```csharp
CreateMap<AttendanceRecord, AttendanceDto>()
    .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student.FullName))
    .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.CourseName))
    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
```

**Why?**
- Reduces boilerplate
- Centralized mapping logic
- Easy to modify without changing handlers
- Type-safe

---

## API Endpoints

### Attendance Management

#### **Create Attendance** (Draft)
```
POST /api/attendance/create
Content-Type: application/json

{
  "studentId": "guid",
  "courseId": "guid",
  "attendanceDate": "2026-01-28",
  "isPresent": true,
  "remarks": "On time",
  "actorId": "guid",
  "actorRole": "Teacher"
}

Response: 201 Created
{
  "id": "guid",
  "studentId": "guid",
  "status": "Draft",
  "isPresent": true,
  ...
}
```

#### **Submit Attendance** (Draft → Submitted)
```
POST /api/attendance/{attendanceId}/submit
Content-Type: application/json

{
  "actorId": "guid",
  "actorRole": "Teacher"
}

Response: 200 OK
```

#### **Approve Attendance** (Submitted → Approved)
```
POST /api/attendance/{attendanceId}/approve
Content-Type: application/json

{
  "actorId": "guid",
  "actorRole": "AcademicCoordinator",
  "approvalNotes": "Verified and approved"
}

Response: 200 OK
```

#### **Publish Attendance** (Approved → Published)
```
POST /api/attendance/{attendanceId}/publish
Content-Type: application/json

{
  "actorId": "guid",
  "actorRole": "AcademicCoordinator"
}

Response: 200 OK
```

#### **Request Reopen** (Submitted/Approved → ReopenRequested)
```
POST /api/attendance/{attendanceId}/request-reopen
Content-Type: application/json

{
  "actorId": "guid",
  "actorRole": "Teacher",
  "reason": "Entry was incorrect, need to update"
}

Response: 200 OK
```

#### **Approve Reopen** (ReopenRequested → Draft)
```
POST /api/attendance/reopen-request/{reopenRequestId}/approve
Content-Type: application/json

{
  "actorId": "guid",
  "actorRole": "AcademicCoordinator",
  "approvalComments": "Approved, teacher can now edit"
}

Response: 200 OK
```

#### **Apply Correction** (Approved/Published → Corrected + New Draft Version)
```
POST /api/attendance/{attendanceId}/apply-correction
Content-Type: application/json

{
  "isPresent": false,
  "remarks": "Corrected - student was absent",
  "correctionReason": "Data entry error found",
  "actorId": "guid",
  "actorRole": "AcademicCoordinator"
}

Response: 201 Created
(Returns new version in Draft state)
```

#### **Lock Attendance** (Published → Locked)
```
POST /api/attendance/{attendanceId}/lock
Content-Type: application/json

{
  "actorId": "guid",
  "actorRole": "AcademicCoordinator"
}

Response: 200 OK
```

### Query Endpoints

#### **Get Single Attendance**
```
GET /api/attendance/{attendanceId}
Response: 200 OK with AttendanceDto
```

#### **Get Attendance by Student & Date**
```
GET /api/attendance/student/{studentId}/date/{date}/course/{courseId}
Response: 200 OK with AttendanceDto
```

#### **Get Student Attendance Range**
```
GET /api/attendance/student/{studentId}/range?startDate=2026-01-01&endDate=2026-01-31
Response: 200 OK with IEnumerable<AttendanceDto>
```

#### **Get Course Attendance for Date**
```
GET /api/attendance/course/{courseId}/date/{date}
Response: 200 OK with IEnumerable<AttendanceDto>
```

#### **Get Attendance by Status**
```
GET /api/attendance/status/{status}
Response: 200 OK with IEnumerable<AttendanceDto>
```

#### **Get All Versions (Including Corrections)**
```
GET /api/attendance/versions/student/{studentId}/date/{date}/course/{courseId}
Response: 200 OK with IEnumerable<AttendanceDto>
```

#### **Get Audit Trail**
```
GET /api/attendance/{attendanceId}/audit-trail
Response: 200 OK with IEnumerable<AuditLogDto>

Returns:
[
  {
    "id": "guid",
    "action": "Created",
    "newStatus": "Draft",
    "actorName": "John Doe (Teacher)",
    "actorRole": "Teacher",
    "reason": "Initial marking",
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

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET Core | 8.0 |
| **Web Framework** | ASP.NET Core | 8.0 |
| **ORM** | Entity Framework Core | 8.0.0 |
| **Database** | SQL Server | Any (can use SQLite for dev) |
| **Command/Query Pattern** | MediatR | 14.0.0 |
| **Mapping** | AutoMapper | 12.0.0 |
| **API Documentation** | Swagger/OpenAPI | 10.1.0 |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection | Built-in |

---

## Setup & Run Instructions

### Prerequisites
- .NET SDK 8.0 or later
- SQL Server LocalDB or any SQL Server instance
- Visual Studio / VS Code / JetBrains Rider (optional)

### Step 1: Clone/Extract Project
```bash
cd c:\Users\<user>\Downloads\DISLAMS\StudentManagementSystem
```

### Step 2: Configure Database Connection
Edit `DISLAMS.StudentManagement.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DISLAMS_StudentManagement;Trusted_Connection=true;"
  }
}
```

### Step 3: Create & Apply Migrations
```bash
cd DISLAMS.StudentManagementSystem

# Add initial migration
dotnet ef migrations add InitialCreate `
  --project DISLAMS.StudentManagement.Infrastructure `
  --startup-project DISLAMS.StudentManagement.API

# Apply migration (creates database)
dotnet ef database update `
  --project DISLAMS.StudentManagement.Infrastructure `
  --startup-project DISLAMS.StudentManagement.API
```

### Step 4: Build Solution
```bash
dotnet build
```

### Step 5: Run API
```bash
dotnet run --project DISLAMS.StudentManagement.API/DISLAMS.StudentManagement.API.csproj
```

### Step 6: Access Swagger UI
Open browser: `https://localhost:5001/swagger/index.html`

---

## Testing the API

### Using Swagger UI
1. Go to `https://localhost:5001/swagger`
2. Click on endpoints to test
3. Try the state transition flow:
   - Create Attendance (Draft)
   - Submit Attendance (Draft → Submitted)
   - Approve Attendance (Submitted → Approved)
   - Publish Attendance (Approved → Published)
   - Get Audit Trail (view full history)

### Using Postman/curl
```bash
# Create attendance
curl -X POST https://localhost:5001/api/attendance/create \
  -H "Content-Type: application/json" \
  -d '{
    "studentId": "12345678-1234-1234-1234-123456789012",
    "courseId": "87654321-4321-4321-4321-210987654321",
    "attendanceDate": "2026-01-28",
    "isPresent": true,
    "remarks": "Present",
    "actorId": "11111111-1111-1111-1111-111111111111",
    "actorRole": "Teacher"
  }'
```

---

## SOLID Principles Implementation

### 1. **Single Responsibility Principle (SRP)**
- **Entity classes** - represent domain objects only
- **Repository classes** - handle data access only
- **Command handlers** - one handler per command
- **Query handlers** - one handler per query
- **Controller** - orchestrate requests, delegate to MediatR

### 2. **Open/Closed Principle (OCP)**
- Can add new commands without modifying existing handlers
- Can add new repositories without modifying existing code
- Can extend entity functionality through inheritance/composition

### 3. **Liskov Substitution Principle (LSP)**
- All repositories implement `IRepository<T>` interface
- Can swap implementations without breaking code
- Audit log repository behaves like repository but prevents updates/deletes

### 4. **Interface Segregation Principle (ISP)**
- `IRepository<T>` - generic operations
- `IAttendanceRepository` - specialized attendance queries
- `IAuditLogRepository` - specialized audit log operations
- Clients don't depend on methods they don't use

### 5. **Dependency Inversion Principle (DIP)**
- Controllers depend on `IMediator` abstraction, not concrete mediator
- Handlers depend on `IRepository` interfaces, not concrete implementations
- Configuration happens at application startup, not hardcoded

---

## Clean Architecture Principles

```
Domain Layer (No dependencies)
    ↑
Application Layer (Depends on Domain)
    ↑
Infrastructure Layer (Depends on Domain)
    ↑
Presentation Layer (Depends on all)
```

**Layer Responsibilities:**

| Layer | Responsibility | Examples |
|-------|---|---|
| **Domain** | Business rules, entities, value objects, interfaces | AttendanceRecord, AttendanceStatus, IRepository |
| **Application** | Use cases, business logic orchestration, command/query handlers | CreateAttendanceCommand, GetAttendanceQuery, MappingProfile |
| **Infrastructure** | Data access, external services, repositories | EF Core DbContext, Repository implementations |
| **Presentation** | HTTP endpoints, request/response handling, error mapping | AttendanceController, API responses |

---

## Governance & Compliance

### What's Audited
- ✅ Who marked attendance
- ✅ When they marked it
- ✅ What role they had
- ✅ What action they took
- ✅ State transition (before → after)
- ✅ Reason for action (if applicable)
- ✅ Timestamp of action
- ✅ Context/environment info

### What's Immutable
- ✅ Audit logs (cannot be edited/deleted)
- ✅ Submitted attendance (can only correct via new version)
- ✅ Published attendance (locked from normal editing)
- ✅ CreatedAt, CreatedBy (entity audit columns)

### What's Versioned
- ✅ Attendance records (corrections create new versions)
- ✅ Complete history preserved
- ✅ Version number tracked
- ✅ Parent reference maintained

### What's Prevented
- ❌ Silent edits (all changes logged)
- ❌ Overwriting historical data (versioning used)
- ❌ Invalid state transitions (state machine enforces)
- ❌ Unauthorized actions (role-based authorization)
- ❌ Audit tampering (immutable logs)

---

## Exception Handling

### Teacher Absent
```csharp
public class AttendanceException
{
    public ExceptionType ExceptionType = ExceptionType.TeacherAbsent;
    public string Description = "Teacher was absent, attendance could not be marked";
}
```

### Late Submission
```csharp
// Checked in SubmitAttendanceHandler
var timeSinceCreation = DateTime.UtcNow - attendance.CreatedAt;
if (timeSinceCreation.TotalHours > 24)
{
    throw new InvalidOperationException("Submission deadline has passed (24 hours).");
}
```

### Parent Dispute
```csharp
// Creates AttendanceException record
var dispute = new AttendanceException(
    attendanceRecordId,
    ExceptionType.ParentDispute,
    "Parent disputes recorded attendance",
    parentId);
```

### System Failure
```csharp
// Tracked as exception, not lost
var systemFailure = new AttendanceException(
    attendanceRecordId,
    ExceptionType.SystemFailure,
    "System failure prevented timely marking",
    systemAdminId);
```

---

## Trade-offs & Intentional Non-Implementation

### What We Did NOT Implement (& Why)

1. **Auto-mark Attendance**
   - ❌ Manual process ensures conscious action
   - ❌ Reduces errors and disputes
   - ✅ Teachers must explicitly mark

2. **Assume Admin is Honest**
   - ❌ We assume NOTHING
   - ✅ Admin actions are audited
   - ✅ Role-based authorization enforced
   - ✅ Audit trail prevents corruption

3. **UI/Frontend**
   - ❌ This is backend-only (per requirements)
   - ✅ API is fully documented with Swagger
   - ✅ Can be consumed by any frontend

4. **Real Authentication**
   - ❌ Hardcoded roles for simplicity
   - ✅ Architecture supports JWT/OAuth
   - ✅ ActorId can come from identity provider

5. **Real-time Notifications**
   - ❌ Out of scope for Phase 2
   - ✅ Audit logs provide history
   - ✅ API clients can check status

6. **Complex Reporting**
   - ❌ Complex reports not in scope
   - ✅ Raw data available via queries
   - ✅ Easy to add reporting layer

---

## Key Files & Their Purpose

| File | Purpose |
|------|---------|
| `AttendanceRecord.cs` | Core entity with versioning & state |
| `AuditLog.cs` | Immutable audit trail |
| `AttendanceStatus.cs` | State machine enum |
| `AttendanceCommands.cs` | CQRS command definitions |
| `AttendanceCommandHandlers.cs` | Business logic for commands |
| `AttendanceQueries.cs` | CQRS query definitions |
| `QueryHandlers.cs` | Business logic for queries |
| `ApplicationDbContext.cs` | EF Core mapping & configuration |
| `AttendanceController.cs` | HTTP endpoints |
| `MappingProfile.cs` | AutoMapper configuration |
| `Program.cs` | DI configuration & middleware |

---

## Future Enhancements

1. **Authentication & Authorization**
   - Integrate with Azure AD / OAuth2
   - JWT token validation
   - Fine-grained role/claim based authorization

2. **Real-time Updates**
   - SignalR for live attendance updates
   - WebSocket for real-time notifications

3. **Reporting & Analytics**
   - Attendance statistics
   - Trend analysis
   - Exception reports

4. **Integration**
   - LMS integration
   - Parent portal
   - SMS notifications

5. **Performance**
   - Caching layer (Redis)
   - Read replicas for queries
   - Database optimization

6. **Advanced Governance**
   - Digital signatures on published records
   - Blockchain audit trail (optional)
   - Multi-level approval workflows

---

## Support & Questions

This system is designed to be:
- **Self-documenting** (Code is clear)
- **Well-structured** (SOLID + Clean Architecture)
- **Auditable** (Complete audit trail)
- **Governance-first** (System cannot be misused)

For questions about design decisions, see the rationale sections above.

---

## License

Internal Use Only - DISLAMS Project

---

**Built with governance, clarity, and system-thinking at heart.**
