using MediatR;
using DISLAMS.StudentManagement.Application.DTOs;

namespace DISLAMS.StudentManagement.Application.Commands
{
    /// <summary>
    /// Create/Mark Attendance Command
    /// Creates a new attendance record in Draft state
    /// Only teachers can execute this
    /// </summary>
    public class CreateAttendanceCommand : IRequest<AttendanceDto>
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
        public string Remarks { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
    }

    /// <summary>
    /// Submit Attendance Command
    /// Transitions record from Draft to Submitted
    /// Only the original teacher can submit
    /// </summary>
    public class SubmitAttendanceCommand : IRequest<AttendanceDto>
    {
        public Guid AttendanceRecordId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
    }

    /// <summary>
    /// Approve Attendance Command
    /// Transitions record from Submitted to Approved
    /// Only Academic Coordinators can approve
    /// </summary>
    public class ApproveAttendanceCommand : IRequest<AttendanceDto>
    {
        public Guid AttendanceRecordId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
        public string ApprovalNotes { get; set; }
    }

    /// <summary>
    /// Publish Attendance Command
    /// Transitions record from Approved to Published
    /// Only Academic Coordinators can publish
    /// Once published, can only be corrected (new version)
    /// </summary>
    public class PublishAttendanceCommand : IRequest<AttendanceDto>
    {
        public Guid AttendanceRecordId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
    }

    /// <summary>
    /// Request Reopen Command
    /// Request to reopen a submitted/approved attendance for correction
    /// Can be initiated by teacher or admin
    /// </summary>
    public class RequestReopenCommand : IRequest<bool>
    {
        public Guid AttendanceRecordId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Approve Reopen Command
    /// Approve a reopen request
    /// Only Academic Coordinators can approve reopens
    /// </summary>
    public class ApproveReopenCommand : IRequest<AttendanceDto>
    {
        public Guid ReopenRequestId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
        public string ApprovalComments { get; set; }
    }

    /// <summary>
    /// Apply Correction Command
    /// Apply a correction to a published attendance record
    /// Creates a new version
    /// Only Academic Coordinators can apply corrections to published records
    /// </summary>
    public class ApplyCorrectionCommand : IRequest<AttendanceDto>
    {
        public Guid AttendanceRecordId { get; set; }
        public bool IsPresent { get; set; }
        public string Remarks { get; set; }
        public string CorrectionReason { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
    }

    /// <summary>
    /// Lock Attendance Command
    /// Transition record to Locked state (final)
    /// Only Academic Coordinators
    /// </summary>
    public class LockAttendanceCommand : IRequest<AttendanceDto>
    {
        public Guid AttendanceRecordId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorRole { get; set; }
    }
}
