using AutoMapper;
using MediatR;
using DISLAMS.StudentManagement.Application.Commands;
using DISLAMS.StudentManagement.Application.DTOs;
using DISLAMS.StudentManagement.Domain.Entities;
using DISLAMS.StudentManagement.Domain.Enums;
using DISLAMS.StudentManagement.Domain.Repositories;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.Application.Handlers
{
    /// <summary>
    /// Create Attendance Command Handler
    /// Creates a new attendance record in Draft state
    /// </summary>
    public class CreateAttendanceCommandHandler : IRequestHandler<CreateAttendanceCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public CreateAttendanceCommandHandler(
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditRepo,
            ApplicationDbContext dbContext,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _auditRepo = auditRepo;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(CreateAttendanceCommand request, CancellationToken cancellationToken)
        {
            // Validate role - only teachers can create attendance
            if (request.ActorRole != "Teacher")
            {
                throw new UnauthorizedAccessException("Only teachers can mark attendance.");
            }

            // Check if student exists
            var student = await _dbContext.Students.FindAsync(new object[] { request.StudentId }, cancellationToken: cancellationToken);
            if (student == null)
                throw new KeyNotFoundException($"Student with ID {request.StudentId} not found.");

            // Check if course exists
            var course = await _dbContext.Courses.FindAsync(new object[] { request.CourseId }, cancellationToken: cancellationToken);
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {request.CourseId} not found.");

            // Check if attendance already exists for this date
            var existingAttendance = await _attendanceRepo.GetByStudentDateCourseAsync(
                request.StudentId, request.AttendanceDate, request.CourseId);
            
            if (existingAttendance != null && existingAttendance.Status != AttendanceStatus.Corrected)
            {
                throw new InvalidOperationException(
                    $"Attendance for student {request.StudentId} on {request.AttendanceDate:yyyy-MM-dd} already exists in state {existingAttendance.Status}");
            }

            // Create new attendance record
            var attendance = new AttendanceRecord(request.StudentId, request.CourseId, request.AttendanceDate)
            {
                IsPresent = request.IsPresent,
                Remarks = request.Remarks ?? string.Empty,
                Status = AttendanceStatus.Draft,
                CreatedBy = request.ActorId,
                ModifiedBy = request.ActorId
            };

            await _attendanceRepo.AddAsync(attendance);
            await _attendanceRepo.SaveChangesAsync();

            // Log the creation
            var auditLog = new AuditLog(
                attendance.Id,
                "Created",
                AttendanceStatus.Draft,
                request.ActorId,
                UserRole.Teacher,
                $"Attendance marked - Present: {request.IsPresent}");

            await _auditRepo.AddAsync(auditLog);
            await _auditRepo.SaveChangesAsync();

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    /// <summary>
    /// Submit Attendance Command Handler
    /// Transitions from Draft to Submitted
    /// </summary>
    public class SubmitAttendanceCommandHandler : IRequestHandler<SubmitAttendanceCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IMapper _mapper;

        public SubmitAttendanceCommandHandler(
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditRepo,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _auditRepo = auditRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(SubmitAttendanceCommand request, CancellationToken cancellationToken)
        {
            // Only teachers can submit
            if (request.ActorRole != "Teacher")
                throw new UnauthorizedAccessException("Only teachers can submit attendance.");

            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Validate state transition - must be in Draft
            if (attendance.Status != AttendanceStatus.Draft)
                throw new InvalidOperationException(
                    $"Cannot submit attendance in {attendance.Status} state. Must be in Draft state.");

            // Check submission deadline (example: within 24 hours)
            var timeSinceCreation = DateTime.UtcNow - attendance.CreatedAt;
            if (timeSinceCreation.TotalHours > 24)
            {
                throw new InvalidOperationException("Submission deadline has passed (24 hours). Request reopen if needed.");
            }

            // Transition to Submitted
            attendance.Status = AttendanceStatus.Submitted;
            attendance.SubmittedAt = DateTime.UtcNow;
            attendance.SubmittedBy = request.ActorId;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);
            await _attendanceRepo.SaveChangesAsync();

            // Audit log
            var auditLog = new AuditLog(
                attendance.Id,
                "Submitted",
                AttendanceStatus.Submitted,
                request.ActorId,
                UserRole.Teacher,
                "Attendance submitted for approval");

            auditLog.PreviousStatus = AttendanceStatus.Draft;

            await _auditRepo.AddAsync(auditLog);
            await _auditRepo.SaveChangesAsync();

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    /// <summary>
    /// Approve Attendance Command Handler
    /// Transitions from Submitted to Approved
    /// </summary>
    public class ApproveAttendanceCommandHandler : IRequestHandler<ApproveAttendanceCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IMapper _mapper;

        public ApproveAttendanceCommandHandler(
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditRepo,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _auditRepo = auditRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(ApproveAttendanceCommand request, CancellationToken cancellationToken)
        {
            // Only Academic Coordinators can approve
            if (request.ActorRole != "AcademicCoordinator")
                throw new UnauthorizedAccessException("Only Academic Coordinators can approve attendance.");

            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Validate state transition
            if (attendance.Status != AttendanceStatus.Submitted)
                throw new InvalidOperationException(
                    $"Cannot approve attendance in {attendance.Status} state. Must be in Submitted state.");

            // Transition to Approved
            attendance.Status = AttendanceStatus.Approved;
            attendance.ApprovedAt = DateTime.UtcNow;
            attendance.ApprovedBy = request.ActorId;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);
            await _attendanceRepo.SaveChangesAsync();

            // Audit log
            var auditLog = new AuditLog(
                attendance.Id,
                "Approved",
                AttendanceStatus.Approved,
                request.ActorId,
                UserRole.AcademicCoordinator,
                request.ApprovalNotes);

            auditLog.PreviousStatus = AttendanceStatus.Submitted;

            await _auditRepo.AddAsync(auditLog);
            await _auditRepo.SaveChangesAsync();

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    /// <summary>
    /// Publish Attendance Command Handler
    /// Transitions from Approved to Published (locked for normal editing)
    /// </summary>
    public class PublishAttendanceCommandHandler : IRequestHandler<PublishAttendanceCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IMapper _mapper;

        public PublishAttendanceCommandHandler(
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditRepo,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _auditRepo = auditRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(PublishAttendanceCommand request, CancellationToken cancellationToken)
        {
            // Only Academic Coordinators can publish
            if (request.ActorRole != "AcademicCoordinator")
                throw new UnauthorizedAccessException("Only Academic Coordinators can publish attendance.");

            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Validate state transition
            if (attendance.Status != AttendanceStatus.Approved)
                throw new InvalidOperationException(
                    $"Cannot publish attendance in {attendance.Status} state. Must be in Approved state.");

            // Transition to Published
            attendance.Status = AttendanceStatus.Published;
            attendance.PublishedAt = DateTime.UtcNow;
            attendance.PublishedBy = request.ActorId;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);
            await _attendanceRepo.SaveChangesAsync();

            // Audit log
            var auditLog = new AuditLog(
                attendance.Id,
                "Published",
                AttendanceStatus.Published,
                request.ActorId,
                UserRole.AcademicCoordinator,
                "Attendance published and locked");

            auditLog.PreviousStatus = AttendanceStatus.Approved;

            await _auditRepo.AddAsync(auditLog);
            await _auditRepo.SaveChangesAsync();

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }
}
