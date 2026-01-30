# DISLAMS Student Management System - Architecture Guide

## Executive Summary

This document explains the architectural decisions behind the DISLAMS Student Management System (Phase 2) - a governance-first attendance management backend built with .NET Core 8, CQRS, Clean Architecture, and EF Core.

**Core Philosophy:** Build systems that cannot be misused.

---

## Table of Contents

1. [System Architecture Overview](#system-architecture-overview)
2. [Domain-Driven Design](#domain-driven-design)
3. [The State Machine](#the-state-machine)
4. [Governance Model](#governance-model)
5. [Immutability & Versioning](#immutability--versioning)
6. [CQRS Pattern](#cqrs-pattern)
7. [Repository Pattern](#repository-pattern)
8. [Dependency Injection](#dependency-injection)
9. [Error Handling & Exceptions](#error-handling--exceptions)
10. [Security & Authorization](#security--authorization)

---

## System Architecture Overview

### Layered Architecture

```
┌─────────────────────────────────────────────────────┐
│         PRESENTATION LAYER (API Controllers)        │
│      - HTTP Endpoints, Request/Response mapping     │
│      - Error handling, status codes                 │
│      - Swagger/OpenAPI documentation               │
└────────────────────┬────────────────────────────────┘
                     │ (depends on)
┌────────────────────▼────────────────────────────────┐
│        APPLICATION LAYER (CQRS, Use Cases)          │
│      - Commands (state-changing operations)        │
│      - Queries (read-only operations)              │
│      - Handlers (business logic orchestration)     │
│      - DTOs (Data Transfer Objects)                │
│      - Mapping (AutoMapper profiles)               │
└────────────────────┬────────────────────────────────┘
                     │ (depends on)
┌────────────────────▼────────────────────────────────┐
│     INFRASTRUCTURE LAYER (Data Access, EF Core)    │
│      - DbContext (EF Core mapping)                │
│      - Repository implementations                  │
│      - Database migrations                         │
└────────────────────┬────────────────────────────────┘
                     │ (depends on)
┌────────────────────▼────────────────────────────────┐
│         DOMAIN LAYER (Business Rules)              │
│      - Entities (domain objects)                   │
│      - Enums (state definitions)                   │
│      - Repository interfaces (abstractions)        │
│      - No external dependencies                    │
└─────────────────────────────────────────────────────┘
```

### Inversion of Dependencies

**Key Principle:** Higher layers depend on abstractions in lower layers, not implementations.

```
Presentation → (depends on) ← Application → (depends on) ← Infrastructure
                            ↓                          ↓
                          Domain (no dependencies, only abstractions)
```

This ensures:
- Domain logic is isolated and testable
- Infrastructure can be swapped (different databases, persistence mechanisms)
- Application logic is framework-agnostic
- Presentation can be replaced (web, console, gRPC)

---

## Domain-Driven Design

### Core Domain Model

The domain is centered around **Attendance Management with Governance**.

**Domain Entities:**

```csharp
// Core domain object
public class AttendanceRecord : Entity
{
    // Immutable identity
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public DateTime AttendanceDate { get; private set; }
    
    // State tracking (versioning)
    public int Version { get; private set; }
    public Guid? ParentVersionId { get; private set; }
    public ICollection<AttendanceRecord> ChildVersions { get; set; }
    
    // Lifecycle tracking
    public AttendanceStatus Status { get; private set; }
    
    // Immutable data (can only be changed via correction/versioning)
    public bool IsPresent { get; private set; }
    public string Remarks { get; private set; }
    
    // Action tracking
    public Guid? SubmittedByActorId { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public Guid? ApprovedByActorId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? PublishedByActorId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    
    // Related data
    public virtual Student Student { get; set; }
    public virtual Course Course { get; set; }
    public virtual ICollection<AuditLog> AuditLogs { get; set; }
    public virtual ICollection<AttendanceException> Exceptions { get; set; }
    public virtual ICollection<ReopenRequest> ReopenRequests { get; set; }
}
```

**Why This Design?**

1. **Immutability of Core Data** - AttendanceDate, StudentId, CourseId cannot change
   - Prevents accidental data loss
   - Maintains referential integrity
   - Audit trail is meaningful

2. **Explicit State Tracking** - Status field drives behavior
   - State machine enforced in handlers
   - Cannot skip invalid transitions
   - Audit logs capture every state change

3. **Action Attribution** - Who did what, when
   - SubmittedByActorId, ApprovedByActorId, etc.
   - Complete accountability
   - Compliance & auditability

4. **Versioning Support** - ParentVersionId, ChildVersions
   - Corrections create new versions, not overwrites
   - Complete history preserved
   - Grandfather-father-son relationships possible

5. **Audit Log Integration** - AuditLogs collection
   - Every action logged
   - Append-only (immutable)
   - Queryable for compliance

---

## The State Machine

### State Definition

```csharp
public enum AttendanceStatus
{
    Draft = 1,            // Created but not submitted
    Submitted = 2,        // Submitted for approval (24-hour window)
    Approved = 3,         // Approved by academic coordinator
    Published = 4,        // Published to permanent record
    Locked = 5,          // Final state, cannot be changed
    ReopenRequested = 6, // Request to reopen for editing
    Corrected = 7        // Original record corrected (new version created)
}
```

### State Transition Rules

```
NORMAL FLOW:
Draft ──Submit──> Submitted ──Approve──> Approved ──Publish──> Published ──Lock──> Locked

REOPEN FLOW:
Submitted ──RequestReopen──> ReopenRequested ──ApproveReopen──> Draft (version resets)
Approved ──RequestReopen──> ReopenRequested ──ApproveReopen──> Draft (version resets)

CORRECTION FLOW:
Published ──ApplyCorrection──> Original marked as "Corrected"
                                + New version created in Draft state
                                
Approved ──ApplyCorrection──> Original marked as "Corrected"
                                + New version created in Draft state
```

### State Transition Enforcement

**In Code (AttendanceCommandHandlers.cs):**

```csharp
// Example: SubmitAttendanceCommand handler
public async Task<AttendanceDto> Handle(SubmitAttendanceCommand request, ...)
{
    // 1. Authorization check
    if (request.ActorRole != "Teacher")
        throw new UnauthorizedAccessException();
    
    // 2. Retrieve entity
    var attendance = await _repository.GetByIdAsync(request.AttendanceRecordId);
    
    // 3. Validate state transition
    if (attendance.Status != AttendanceStatus.Draft)
        throw new InvalidOperationException($"Can only submit Draft attendance, current status: {attendance.Status}");
    
    // 4. Business rule validation
    var timeSinceCreation = DateTime.UtcNow - attendance.CreatedAt;
    if (timeSinceCreation.TotalHours > 24)
        throw new InvalidOperationException("Submission deadline has passed (24 hours).");
    
    // 5. State transition
    attendance.Status = AttendanceStatus.Submitted;
    attendance.SubmittedByActorId = request.ActorId;
    attendance.SubmittedAt = DateTime.UtcNow;
    
    // 6. Audit logging
    var auditLog = new AuditLog(
        action: "Submitted",
        previousStatus: AttendanceStatus.Draft,
        newStatus: AttendanceStatus.Submitted,
        actorId: request.ActorId,
        actorRole: request.ActorRole,
        reason: "Submitted for approval");
    
    // 7. Persistence
    await _repository.UpdateAsync(attendance);
    await _auditLogRepository.AddAsync(auditLog);
    
    // 8. Return DTO
    return _mapper.Map<AttendanceDto>(attendance);
}
```

### Why This Design?

1. **Explicit Transitions** - State machine prevents invalid states
2. **Single Responsibility** - Each handler knows one transition
3. **Auditability** - Every state change logged
4. **Rollback Prevention** - Cannot go backward without reopen request
5. **Time Boundaries** - 24-hour submission window enforced
6. **Role-Based** - Different roles can only perform specific transitions

---

## Governance Model

### Role-Based Access Control

```
ROLE            ACTIONS
┌──────────────────────────────────────────────────────┐
│ Teacher        │ Create, Submit, RequestReopen      │
├──────────────────────────────────────────────────────┤
│ Academic       │ Approve, Publish, ApplyCorrection  │
│ Coordinator    │ ApproveReopen, Lock               │
├──────────────────────────────────────────────────────┤
│ Leadership     │ View/Report only (no mutations)    │
└──────────────────────────────────────────────────────┘
```

### Authorization Points

Every command handler checks authorization:

```csharp
// Pattern used in every handler
public async Task<AttendanceDto> Handle(SomeCommand request, ...)
{
    // FIRST check: Authorization
    if (!IsAuthorizedForAction(request.ActorRole, request.Action))
        throw new UnauthorizedAccessException($"Role {request.ActorRole} cannot {request.Action}");
    
    // THEN proceed with business logic
    ...
}
```

### Non-Repudiation

Every action captured with:
- **WHO** - ActorId, ActorRole
- **WHAT** - Action name, state transition
- **WHEN** - Timestamp
- **WHERE** - ContextInfo
- **WHY** - Reason/remarks

---

## Immutability & Versioning

### Problem Statement

**Without Versioning:**
```
Teacher creates attendance: Present = true
Teacher realizes mistake, edits: Present = false

ISSUE: No record of the mistake or correction!
       The original "true" is lost.
```

**Without Immutability:**
```
Published attendance (final decision) can be edited
Coordinator accidentally changes a published record
No audit trail of what was changed

ISSUE: System integrity compromised!
```

### Solution: Versioning Strategy

**When Correction Needed:**

```csharp
// Original record (Published)
var original = await _repository.GetByIdAsync(recordId);
// original.IsPresent = true
// original.Status = AttendanceStatus.Published
// original.Version = 1

// Create new version
var newVersion = original.CreateCorrectionVersion();
// newVersion.IsPresent = false  ← corrected data
// newVersion.Status = AttendanceStatus.Draft  ← back to draft for review
// newVersion.Version = 2
// newVersion.ParentVersionId = original.Id

// Mark original as corrected
original.Status = AttendanceStatus.Corrected;

// Save both
await _repository.UpdateAsync(original);
await _repository.AddAsync(newVersion);
```

**Benefits:**
1. Original data preserved (history)
2. Correction is new record (complete audit trail)
3. Can revert if needed (original still exists)
4. Compliance friendly (no overwrites)
5. Query all versions: `GetAllVersionsAsync(studentId, date, courseId)`

### Immutability Enforcement

**Audit Logs - Append Only:**

```csharp
public class AuditLogRepository : Repository<AuditLog>
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

**Attendance Record - Controlled Changes:**

Once submitted, AttendanceRecord fields are private with no setters:

```csharp
public class AttendanceRecord : Entity
{
    // Read-only properties - can only be set at creation
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public DateTime AttendanceDate { get; private set; }
    public bool IsPresent { get; private set; }
    public string Remarks { get; private set; }
    
    // Only state and tracking can be modified via handlers
    public AttendanceStatus Status { get; private set; }
    
    // Modification only through factory methods
    public AttendanceRecord CreateCorrectionVersion()
    {
        return new AttendanceRecord
        {
            StudentId = this.StudentId,
            CourseId = this.CourseId,
            AttendanceDate = this.AttendanceDate,
            IsPresent = this.IsPresent,
            Remarks = this.Remarks,
            Version = this.Version + 1,
            ParentVersionId = this.Id,
            Status = AttendanceStatus.Draft
        };
    }
}
```

---

## CQRS Pattern

### Concept

**CQRS = Command Query Responsibility Segregation**

Separate read operations (queries) from write operations (commands).

```
                    ┌────────────────┐
                    │  Controller    │
                    └────────┬───────┘
                             │
                    ┌────────▼───────┐
                    │  MediatR Hub   │
                    └────┬───────┬───┘
                         │       │
        ┌────────────────┘       └──────────────────┐
        │                                           │
        ▼                                           ▼
    ┌─────────┐                                ┌────────┐
    │ Commands│   (State-Changing)             │Queries │  (Read-Only)
    ├─────────┤                                ├────────┤
    │ Create  │                                │ Get    │
    │ Submit  │                                │ List   │
    │ Approve │                                │ Search │
    │ Publish │                                │ Report │
    │ Lock    │                                └────────┘
    └────┬────┘
         │
         ▼
    ┌─────────────────┐
    │ Repositories    │
    │ Database Update │
    └─────────────────┘
```

### Why CQRS?

1. **Separation of Concerns**
   - Commands handle logic changes (complex, heavily validated)
   - Queries handle reads (simple, fast)

2. **Scalability**
   - Can optimize each separately
   - Queries can use read-optimized databases
   - Commands hit transactional database

3. **Testability**
   - Each command handler is a unit
   - Each query handler is a unit
   - Mocking is straightforward

4. **Clarity**
   - Obvious what changes system state
   - Obvious what is read-only
   - Code intent is clear

### Implementation in DISLAMS

**Commands (8 total):**

```csharp
// State-changing
public class CreateAttendanceCommand : IRequest<AttendanceDto> { ... }
public class SubmitAttendanceCommand : IRequest<AttendanceDto> { ... }
public class ApproveAttendanceCommand : IRequest<AttendanceDto> { ... }
public class PublishAttendanceCommand : IRequest<AttendanceDto> { ... }
public class RequestReopenCommand : IRequest<bool> { ... }
public class ApproveReopenCommand : IRequest<AttendanceDto> { ... }
public class ApplyCorrectionCommand : IRequest<AttendanceDto> { ... }
public class LockAttendanceCommand : IRequest<AttendanceDto> { ... }
```

**Queries (7 total):**

```csharp
// Read-only
public class GetAttendanceQuery : IRequest<AttendanceDto> { ... }
public class GetByStudentDateQuery : IRequest<AttendanceDto> { ... }
public class GetStudentAttendanceRangeQuery : IRequest<IEnumerable<AttendanceDto>> { ... }
public class GetCourseAttendanceForDateQuery : IRequest<IEnumerable<AttendanceDto>> { ... }
public class GetByStatusQuery : IRequest<IEnumerable<AttendanceDto>> { ... }
public class GetVersionsQuery : IRequest<IEnumerable<AttendanceDto>> { ... }
public class GetAuditTrailQuery : IRequest<IEnumerable<AuditLogDto>> { ... }
```

**Usage in Controller:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceCommand command)
    {
        // Dispatch to handler via MediatR
        var result = await _mediator.Send(command);
        return Created("", result);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAttendance(Guid id)
    {
        // Dispatch to handler via MediatR
        var query = new GetAttendanceQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

---

## Repository Pattern

### Purpose

Abstract data access from business logic. Controllers and handlers don't know about EF Core or SQL - they use repositories.

### Implementation

**Generic Repository Interface:**

```csharp
public interface IRepository<T> where T : Entity
{
    Task<T> GetByIdAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
```

**Generic Repository Implementation:**

```csharp
public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public async Task<T> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    public async Task<T> AddAsync(T entity)
    {
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
```

**Specialized Repository:**

```csharp
public interface IAttendanceRepository : IRepository<AttendanceRecord>
{
    // Domain-specific queries
    Task<AttendanceRecord> GetByStudentDateCourseAsync(Guid studentId, DateTime date, Guid courseId);
    Task<IEnumerable<AttendanceRecord>> GetAllVersionsAsync(Guid studentId, DateTime date, Guid courseId);
    Task<AttendanceRecord> GetLatestVersionAsync(Guid studentId, DateTime date, Guid courseId);
    Task<IEnumerable<AttendanceRecord>> GetByStudentAndDateRangeAsync(Guid studentId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<AttendanceRecord>> GetByCourseAndDateAsync(Guid courseId, DateTime date);
    Task<IEnumerable<AttendanceRecord>> GetByStatusAsync(AttendanceStatus status);
    Task<IEnumerable<AttendanceRecord>> GetByStatusesAsync(params AttendanceStatus[] statuses);
}
```

### Benefits

1. **Abstraction** - Business logic doesn't know about EF Core
2. **Testability** - Can mock repositories in unit tests
3. **Swappability** - Can change database without changing business logic
4. **Consistency** - All data access goes through same interface
5. **Query Reusability** - Complex queries in one place

---

## Dependency Injection

### Configuration (Program.cs)

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Add MediatR (CQRS)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migration on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.Run();
```

### Dependency Graph

```
Controller
  ↓ depends on
IMediator (injected from DI container)
  ↓ depends on
CommandHandler (auto-discovered by MediatR)
  ↓ depends on
IAttendanceRepository (injected from DI container)
  ↓ depends on
ApplicationDbContext (injected from DI container)
  ↓ depends on
SqlServer (connection string from config)
```

### Benefits

1. **Loose Coupling** - Dependencies are abstractions
2. **Testability** - Can inject mocks in tests
3. **Configuration** - Lifetime management (Scoped, Singleton, Transient)
4. **Auto-discovery** - MediatR finds handlers automatically
5. **Flexibility** - Can change implementations without code changes

---

## Error Handling & Exceptions

### Exception Types

1. **UnauthorizedAccessException** - Role violation
2. **InvalidOperationException** - Invalid state transition
3. **ArgumentException** - Invalid input
4. **EntityNotFoundException** - Record not found

### Handler Pattern

```csharp
public async Task<IActionResult> SubmitAttendance(Guid id, [FromBody] SubmitRequest request)
{
    try
    {
        var command = new SubmitAttendanceCommand
        {
            AttendanceRecordId = id,
            ActorId = request.ActorId,
            ActorRole = request.ActorRole
        };
        
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "Internal server error" });
    }
}
```

### HTTP Status Codes

| Status | Meaning | Example |
|--------|---------|---------|
| 200 OK | Success | Attendance approved successfully |
| 201 Created | Resource created | Attendance created |
| 400 Bad Request | Invalid input | Invalid state transition |
| 401 Unauthorized | Authorization failed | Teacher tried to approve |
| 404 Not Found | Resource not found | Attendance ID doesn't exist |
| 500 Server Error | Unhandled exception | Database connection failed |

---

## Security & Authorization

### Current Implementation

**Hardcoded Roles (for simplicity):**

```csharp
// In command payload
{
  "actorId": "11111111-1111-1111-1111-111111111111",
  "actorRole": "Teacher"  // or "AcademicCoordinator"
}
```

**Validated in Handler:**

```csharp
if (request.ActorRole != "AcademicCoordinator")
    throw new UnauthorizedAccessException("Only Academic Coordinators can approve.");
```

### Future: Real Authentication

Could integrate with:
1. **Azure AD** - Enterprise authentication
2. **OAuth2** - Third-party login
3. **JWT Tokens** - Stateless authentication
4. **Claims-Based Authorization** - Fine-grained permissions

### Current Security Measures

1. ✅ Role-based command validation
2. ✅ Immutable audit logs (cannot be deleted)
3. ✅ State machine enforcement (cannot skip states)
4. ✅ Action attribution (who did what)
5. ✅ Timestamp tracking (when actions occurred)
6. ⚠️ Input validation (basic, can be enhanced)
7. ⚠️ SQL injection prevention (EF Core handles)
8. ⚠️ HTTPS enforcement (configure in production)

---

## Data Flow Example: Complete Attendance Workflow

### Step 1: Create Attendance

```
Teacher submits:
POST /api/attendance/create
{
  "studentId": "...",
  "courseId": "...",
  "attendanceDate": "2026-01-28",
  "isPresent": true,
  "remarks": "Present",
  "actorId": "...",
  "actorRole": "Teacher"
}

↓
Controller receives CreateAttendanceCommand
↓
MediatR routes to CreateAttendanceCommandHandler
↓
Handler validates:
  - Role is "Teacher" ✓
  - Student exists ✓
  - Course exists ✓
  - No duplicate for this date/course ✓
↓
Handler creates:
  - AttendanceRecord (Status = Draft)
  - AuditLog (Action = "Created", PreviousStatus = null, NewStatus = Draft)
↓
Database updated:
  INSERT INTO AttendanceRecords (StudentId, CourseId, AttendanceDate, Status, IsPresent, ...)
  INSERT INTO AuditLogs (Action, NewStatus, ActorId, ActorRole, ...)
↓
Returns AttendanceDto with Id and Status = "Draft"
```

### Step 2: Submit Attendance

```
Teacher submits:
POST /api/attendance/{id}/submit
{
  "actorId": "...",
  "actorRole": "Teacher"
}

↓
Controller receives SubmitAttendanceCommand
↓
MediatR routes to SubmitAttendanceCommandHandler
↓
Handler validates:
  - Role is "Teacher" ✓
  - Current status is "Draft" ✓
  - Within 24 hours of creation ✓
↓
Handler transitions:
  - Status: Draft → Submitted
  - SubmittedByActorId = request.ActorId
  - SubmittedAt = DateTime.UtcNow
↓
Handler logs:
  - AuditLog (Action = "Submitted", PreviousStatus = Draft, NewStatus = Submitted)
↓
Database updated:
  UPDATE AttendanceRecords SET Status = 2, SubmittedAt = ..., ...
  INSERT INTO AuditLogs (Action = "Submitted", ...)
↓
Returns updated AttendanceDto
```

### Step 3: Approve Attendance

```
Academic Coordinator submits:
POST /api/attendance/{id}/approve
{
  "actorId": "...",
  "actorRole": "AcademicCoordinator",
  "approvalNotes": "Verified and approved"
}

↓
Handler validates:
  - Role is "AcademicCoordinator" ✓
  - Current status is "Submitted" ✓
↓
Handler transitions:
  - Status: Submitted → Approved
  - ApprovedByActorId = request.ActorId
  - ApprovedAt = DateTime.UtcNow
↓
Handler logs:
  - AuditLog with approvalNotes in Reason field
↓
Database updated & returns AttendanceDto
```

### Step 4: Publish Attendance

```
Academic Coordinator submits:
POST /api/attendance/{id}/publish
{
  "actorId": "...",
  "actorRole": "AcademicCoordinator"
}

↓
Handler validates:
  - Role is "AcademicCoordinator" ✓
  - Current status is "Approved" ✓
↓
Handler transitions:
  - Status: Approved → Published
  - PublishedByActorId = request.ActorId
  - PublishedAt = DateTime.UtcNow
↓
Handler logs:
  - AuditLog marking as published (final state)
↓
Record is now permanent (locked from normal editing)
```

### Step 5: Query Audit Trail

```
GET /api/attendance/{id}/audit-trail

↓
MediatR routes to GetAuditTrailQueryHandler
↓
Handler queries:
  SELECT * FROM AuditLogs WHERE AttendanceRecordId = @id
  JOIN Actors to get actor names
↓
Returns array of AuditLogDtos with complete history:
[
  {
    "action": "Created",
    "newStatus": "Draft",
    "actorName": "John Doe (Teacher)",
    "actionTimestamp": "2026-01-28T10:00:00Z"
  },
  {
    "action": "Submitted",
    "previousStatus": "Draft",
    "newStatus": "Submitted",
    "actorName": "John Doe (Teacher)",
    "actionTimestamp": "2026-01-28T11:00:00Z"
  },
  {
    "action": "Approved",
    "previousStatus": "Submitted",
    "newStatus": "Approved",
    "actorName": "Jane Smith (AcademicCoordinator)",
    "actionTimestamp": "2026-01-28T15:00:00Z"
  },
  {
    "action": "Published",
    "previousStatus": "Approved",
    "newStatus": "Published",
    "actorName": "Jane Smith (AcademicCoordinator)",
    "actionTimestamp": "2026-01-28T16:00:00Z"
  }
]
```

---

## Performance Considerations

### Database Indexes

Defined in `ApplicationDbContext.OnModelCreating`:

```csharp
// Performance index on frequently queried columns
modelBuilder.Entity<AttendanceRecord>()
    .HasIndex(a => a.Status)
    .HasName("IX_AttendanceRecords_Status");

modelBuilder.Entity<AttendanceRecord>()
    .HasIndex(a => a.AttendanceDate)
    .HasName("IX_AttendanceRecords_AttendanceDate");

// Composite index for common query pattern
modelBuilder.Entity<AttendanceRecord>()
    .HasIndex(a => new { a.StudentId, a.AttendanceDate, a.CourseId })
    .IsUnique()
    .HasName("IX_AttendanceRecords_Student_Date_Course");
```

### Query Optimization

```csharp
// In repository - uses indexes
public async Task<AttendanceRecord> GetByStudentDateCourseAsync(Guid studentId, DateTime date, Guid courseId)
{
    return await _dbSet
        .Include(a => a.Student)
        .Include(a => a.Course)
        .FirstOrDefaultAsync(a => 
            a.StudentId == studentId && 
            a.AttendanceDate == date && 
            a.CourseId == courseId);
}
```

### Caching Opportunities (Future)

1. Student/Course lookup (rarely changes)
2. Published attendance (immutable)
3. Audit logs (append-only)

---

## Testing Strategy

### Unit Tests (Should be added)

```csharp
[TestClass]
public class CreateAttendanceCommandHandlerTests
{
    private Mock<IAttendanceRepository> _mockRepository;
    private CreateAttendanceCommandHandler _handler;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IAttendanceRepository>();
        _handler = new CreateAttendanceCommandHandler(_mockRepository.Object);
    }
    
    [TestMethod]
    public async Task Handle_WithValidTeacher_CreatesAttendance()
    {
        // Arrange
        var command = new CreateAttendanceCommand
        {
            StudentId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            ActorRole = "Teacher"
        };
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.AreEqual(AttendanceStatus.Draft, result.Status);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AttendanceRecord>()), Times.Once);
    }
    
    [TestMethod]
    [ExpectedException(typeof(UnauthorizedAccessException))]
    public async Task Handle_WithNonTeacher_ThrowsUnauthorized()
    {
        // Arrange
        var command = new CreateAttendanceCommand
        {
            ActorRole = "Student"  // Invalid role
        };
        
        // Act
        await _handler.Handle(command, CancellationToken.None);
    }
}
```

### Integration Tests (Should be added)

```csharp
[TestClass]
public class AttendanceWorkflowIntegrationTests
{
    private ApplicationDbContext _dbContext;
    private IMediator _mediator;
    
    [TestMethod]
    public async Task CompleteWorkflow_CreateSubmitApprovePublish_Succeeds()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        
        // Act - Create
        var createCommand = new CreateAttendanceCommand { ... };
        var attendanceDto = await _mediator.Send(createCommand);
        
        // Act - Submit
        var submitCommand = new SubmitAttendanceCommand { ... };
        await _mediator.Send(submitCommand);
        
        // Act - Approve
        var approveCommand = new ApproveAttendanceCommand { ... };
        await _mediator.Send(approveCommand);
        
        // Act - Publish
        var publishCommand = new PublishAttendanceCommand { ... };
        await _mediator.Send(publishCommand);
        
        // Assert
        var audit = await _mediator.Send(new GetAuditTrailQuery { ... });
        Assert.AreEqual(4, audit.Count); // 4 state transitions
    }
}
```

---

## Summary

This architecture delivers:

✅ **Governance-First Design** - Every action controlled, audited, and non-repudiable
✅ **Data Integrity** - Immutability and versioning prevent data loss
✅ **Clear Accountability** - Complete audit trail of who did what when
✅ **Scalability** - CQRS allows optimization of reads/writes separately
✅ **Testability** - Dependency injection and clear interfaces
✅ **Maintainability** - Clean architecture, SOLID principles, separation of concerns
✅ **Compliance** - Role-based authorization, state machine enforcement, audit logs
✅ **Non-Repudiation** - Actions cannot be denied or deleted

The system is built not for ease of use, but for **safety, clarity, and governance** - preventing misuse through design rather than policy.

---

**"Who can build systems that cannot be misused."**
