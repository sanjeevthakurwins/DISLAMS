using DISLAMS.StudentManagement.Domain.Enums;

namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Represents an exception or special case in attendance
    /// 
    /// Examples:
    /// - Teacher was absent (cannot mark attendance)
    /// - Parent disputes recorded attendance
    /// - System failure prevented marking
    /// - Late submission
    /// </summary>
    public class AttendanceException : Entity
    {
        /// <summary>
        /// Reference to the attendance record
        /// </summary>
        public Guid AttendanceRecordId { get; set; }
        public virtual AttendanceRecord AttendanceRecord { get; set; }

        /// <summary>
        /// Type of exception
        /// </summary>
        public ExceptionType ExceptionType { get; set; }

        /// <summary>
        /// Description of the exception
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// When the exception was reported
        /// </summary>
        public DateTime ReportedAt { get; set; }

        /// <summary>
        /// Who reported the exception
        /// </summary>
        public Guid ReportedBy { get; set; }

        /// <summary>
        /// Resolution status (resolved/pending)
        /// </summary>
        public string ResolutionStatus { get; set; }

        /// <summary>
        /// How the exception was resolved
        /// </summary>
        public string ResolutionNotes { get; set; }

        /// <summary>
        /// When the exception was resolved
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Who resolved the exception
        /// </summary>
        public Guid? ResolvedBy { get; set; }

        public AttendanceException()
        {
            Description = string.Empty;
            ResolutionStatus = "Pending";
            ResolutionNotes = string.Empty;
            ReportedAt = DateTime.UtcNow;
        }

        public AttendanceException(
            Guid attendanceRecordId,
            ExceptionType exceptionType,
            string description,
            Guid reportedBy) : this()
        {
            AttendanceRecordId = attendanceRecordId;
            ExceptionType = exceptionType;
            Description = description;
            ReportedBy = reportedBy;
        }
    }
}
