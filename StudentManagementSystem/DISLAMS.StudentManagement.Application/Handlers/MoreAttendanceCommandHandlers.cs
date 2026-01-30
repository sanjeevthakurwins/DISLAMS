using MediatR;
using AutoMapper;
using DISLAMS.StudentManagement.Application.Commands;
using DISLAMS.StudentManagement.Application.DTOs;
using DISLAMS.StudentManagement.Domain.Entities;
using DISLAMS.StudentManagement.Domain.Enums;
using DISLAMS.StudentManagement.Domain.Repositories;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.Application.Handlers
{
    /// <summary>
    /// Request Reopen Command Handler
    /// Create a reopen request for an attendance record
    /// </summary>
    public class RequestReopenCommandHandler : IRequestHandler<RequestReopenCommand, bool>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAuditLogRepository _auditRepo;

        public RequestReopenCommandHandler(
            IAttendanceRepository attendanceRepo,
            ApplicationDbContext dbContext,
            IAuditLogRepository auditRepo)
        {
            _attendanceRepo = attendanceRepo;
            _dbContext = dbContext;
            _auditRepo = auditRepo;
        }

        public async Task<bool> Handle(RequestReopenCommand request, CancellationToken cancellationToken)
        {
            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Can only request reopen if in Submitted or Approved state
            if (attendance.Status != AttendanceStatus.Submitted && attendance.Status != AttendanceStatus.Approved)
                throw new InvalidOperationException(
                    $"Cannot request reopen for attendance in {attendance.Status} state. Must be Submitted or Approved.");

            // Change status to ReopenRequested
            var previousStatus = attendance.Status;
            attendance.Status = AttendanceStatus.ReopenRequested;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);

            // Create reopen request
            var reopenRequest = new ReopenRequest(request.AttendanceRecordId, request.Reason, request.ActorId);
            _dbContext.ReopenRequests.Add(reopenRequest);

            // Audit log
            var auditLog = new AuditLog(
                attendance.Id,
                "ReopenRequested",
                AttendanceStatus.ReopenRequested,
                request.ActorId,
                request.ActorRole == "Teacher" ? UserRole.Teacher : UserRole.AcademicCoordinator,
                request.Reason);

            auditLog.PreviousStatus = previousStatus;

            _dbContext.AuditLogs.Add(auditLog);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// Approve Reopen Command Handler
    /// Approve a reopen request and transition record back to Draft
    /// </summary>
    public class ApproveReopenCommandHandler : IRequestHandler<ApproveReopenCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IMapper _mapper;

        public ApproveReopenCommandHandler(
            IAttendanceRepository attendanceRepo,
            ApplicationDbContext dbContext,
            IAuditLogRepository auditRepo,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _dbContext = dbContext;
            _auditRepo = auditRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(ApproveReopenCommand request, CancellationToken cancellationToken)
        {
            // Only Academic Coordinators can approve reopens
            if (request.ActorRole != "AcademicCoordinator")
                throw new UnauthorizedAccessException("Only Academic Coordinators can approve reopen requests.");

            var reopenRequest = await _dbContext.ReopenRequests.FindAsync(new object[] { request.ReopenRequestId }, cancellationToken: cancellationToken);
            if (reopenRequest == null)
                throw new KeyNotFoundException("Reopen request not found.");

            var attendance = await _attendanceRepo.GetByIdAsync(reopenRequest.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Validate status
            if (attendance.Status != AttendanceStatus.ReopenRequested)
                throw new InvalidOperationException(
                    $"Cannot approve reopen for attendance in {attendance.Status} state. Must be ReopenRequested.");

            // Transition back to Draft
            var previousStatus = attendance.Status;
            attendance.Status = AttendanceStatus.Draft;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);

            // Update reopen request
            reopenRequest.Status = "Approved";
            reopenRequest.ApprovedBy = request.ActorId;
            reopenRequest.ApprovedAt = DateTime.UtcNow;
            reopenRequest.ApprovalComments = request.ApprovalComments;

            _dbContext.ReopenRequests.Update(reopenRequest);

            // Audit log
            var auditLog = new AuditLog(
                attendance.Id,
                "ReopenApproved",
                AttendanceStatus.Draft,
                request.ActorId,
                UserRole.AcademicCoordinator,
                $"Reopen approved. Comments: {request.ApprovalComments}");

            auditLog.PreviousStatus = previousStatus;

            _dbContext.AuditLogs.Add(auditLog);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }

    /// <summary>
    /// Apply Correction Command Handler
    /// Create a new version of the attendance record with corrections
    /// </summary>
    public class ApplyCorrectionCommandHandler : IRequestHandler<ApplyCorrectionCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IMapper _mapper;

        public ApplyCorrectionCommandHandler(
            IAttendanceRepository attendanceRepo,
            ApplicationDbContext dbContext,
            IAuditLogRepository auditRepo,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _dbContext = dbContext;
            _auditRepo = auditRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(ApplyCorrectionCommand request, CancellationToken cancellationToken)
        {
            // Only Academic Coordinators can apply corrections to published records
            if (request.ActorRole != "AcademicCoordinator")
                throw new UnauthorizedAccessException("Only Academic Coordinators can apply corrections.");

            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Can only correct published or approved records
            if (attendance.Status != AttendanceStatus.Published && attendance.Status != AttendanceStatus.Approved)
                throw new InvalidOperationException(
                    $"Cannot correct attendance in {attendance.Status} state. Must be Published or Approved.");

            // Mark original as Corrected
            attendance.Status = AttendanceStatus.Corrected;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);

            // Create new version
            var correction = attendance.CreateCorrectionVersion();
            correction.IsPresent = request.IsPresent;
            correction.Remarks = request.Remarks ?? string.Empty;
            correction.CreatedBy = request.ActorId;
            correction.ModifiedBy = request.ActorId;
            correction.Status = AttendanceStatus.Draft; // New version starts as Draft

            await _attendanceRepo.AddAsync(correction);
            await _attendanceRepo.SaveChangesAsync();

            // Audit log on original
            var originalAuditLog = new AuditLog(
                attendance.Id,
                "Corrected",
                AttendanceStatus.Corrected,
                request.ActorId,
                UserRole.AcademicCoordinator,
                $"Correction applied. Reason: {request.CorrectionReason}");

            await _auditRepo.AddAsync(originalAuditLog);

            // Audit log on correction
            var correctionAuditLog = new AuditLog(
                correction.Id,
                "CorrectionCreated",
                AttendanceStatus.Draft,
                request.ActorId,
                UserRole.AcademicCoordinator,
                $"New correction version created. Reason: {request.CorrectionReason}");

            correctionAuditLog.PreviousValue = $"IsPresent: {attendance.IsPresent}, Remarks: {attendance.Remarks}";
            correctionAuditLog.NewValue = $"IsPresent: {correction.IsPresent}, Remarks: {correction.Remarks}";

            await _auditRepo.AddAsync(correctionAuditLog);
            await _auditRepo.SaveChangesAsync();

            return _mapper.Map<AttendanceDto>(correction);
        }
    }

    /// <summary>
    /// Lock Attendance Command Handler
    /// Transition from Published to Locked (final state)
    /// </summary>
    public class LockAttendanceCommandHandler : IRequestHandler<LockAttendanceCommand, AttendanceDto>
    {
        private readonly IAttendanceRepository _attendanceRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IMapper _mapper;

        public LockAttendanceCommandHandler(
            IAttendanceRepository attendanceRepo,
            IAuditLogRepository auditRepo,
            IMapper mapper)
        {
            _attendanceRepo = attendanceRepo;
            _auditRepo = auditRepo;
            _mapper = mapper;
        }

        public async Task<AttendanceDto> Handle(LockAttendanceCommand request, CancellationToken cancellationToken)
        {
            // Only Academic Coordinators can lock
            if (request.ActorRole != "AcademicCoordinator")
                throw new UnauthorizedAccessException("Only Academic Coordinators can lock attendance.");

            var attendance = await _attendanceRepo.GetByIdAsync(request.AttendanceRecordId);
            if (attendance == null)
                throw new KeyNotFoundException("Attendance record not found.");

            // Can only lock published records
            if (attendance.Status != AttendanceStatus.Published)
                throw new InvalidOperationException(
                    $"Cannot lock attendance in {attendance.Status} state. Must be Published.");

            // Transition to Locked
            var previousStatus = attendance.Status;
            attendance.Status = AttendanceStatus.Locked;
            attendance.ModifiedBy = request.ActorId;
            attendance.ModifiedAt = DateTime.UtcNow;

            await _attendanceRepo.UpdateAsync(attendance);
            await _attendanceRepo.SaveChangesAsync();

            // Audit log
            var auditLog = new AuditLog(
                attendance.Id,
                "Locked",
                AttendanceStatus.Locked,
                request.ActorId,
                UserRole.AcademicCoordinator,
                "Attendance locked - no further changes allowed");

            auditLog.PreviousStatus = previousStatus;

            await _auditRepo.AddAsync(auditLog);
            await _auditRepo.SaveChangesAsync();

            return _mapper.Map<AttendanceDto>(attendance);
        }
    }
}
