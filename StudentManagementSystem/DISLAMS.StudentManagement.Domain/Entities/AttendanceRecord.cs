using DISLAMS.StudentManagement.Domain.Enums;

namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Core Attendance Record entity
    /// This is the PRIMARY RECORD of a student's attendance session.
    /// 
    /// Key Governance Rules:
    /// 1. IMMUTABLE ONCE SUBMITTED - Cannot be edited, only corrected through version
    /// 2. STATE MACHINE - Follows strict state transitions
    /// 3. AUDIT TRAIL - Every action is logged
    /// 4. NO SILENT EDITS - All changes create new versions or audit entries
    /// 
    /// Versioning Strategy:
    /// - Attendance records have versions
    /// - When a correction is applied, a new version is created
    /// - Old versions remain immutable
    /// - The latest version is the current record
    /// </summary>
    public class AttendanceRecord : Entity
    {
        /// <summary>
        /// Reference to the student (immutable)
        /// </summary>
        public Guid StudentId { get; set; }
        public virtual Student Student { get; set; }

        /// <summary>
        /// Reference to the course/class (immutable)
        /// </summary>
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; }

        /// <summary>
        /// The date for which attendance is being recorded (immutable)
        /// </summary>
        public DateTime AttendanceDate { get; set; }

        /// <summary>
        /// Current state of this attendance record (mutable via state machine)
        /// </summary>
        public AttendanceStatus Status { get; set; }

        /// <summary>
        /// True if student is marked present
        /// False if student is marked absent
        /// Note: This is immutable - changes via corrections
        /// </summary>
        public bool IsPresent { get; set; }

        /// <summary>
        /// Optional remarks (e.g., late arrival, approved leave)
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Version number (0 = original, 1+ = corrections)
        /// Incremented when a correction is applied
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Reference to parent record if this is a correction version
        /// Null if this is the original record
        /// </summary>
        public Guid? ParentVersionId { get; set; }
        public virtual AttendanceRecord? ParentVersion { get; set; }

        /// <summary>
        /// Child versions (corrections) of this record
        /// </summary>
        public virtual ICollection<AttendanceRecord> ChildVersions { get; set; } = new List<AttendanceRecord>();

        /// <summary>
        /// Timestamp when this record was submitted
        /// Null until submitted
        /// </summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>
        /// User ID who submitted this record
        /// </summary>
        public Guid? SubmittedBy { get; set; }

        /// <summary>
        /// Timestamp when this record was approved
        /// Null until approved
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// User ID who approved this record
        /// </summary>
        public Guid? ApprovedBy { get; set; }

        /// <summary>
        /// Timestamp when this record was published
        /// Null until published
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// User ID who published this record
        /// </summary>
        public Guid? PublishedBy { get; set; }

        /// <summary>
        /// Audit log entries for this record
        /// </summary>
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        /// <summary>
        /// Exceptions/special cases for this record
        /// </summary>
        public virtual ICollection<AttendanceException> Exceptions { get; set; } = new List<AttendanceException>();

        /// <summary>
        /// Reopen requests for this record
        /// </summary>
        public virtual ICollection<ReopenRequest> ReopenRequests { get; set; } = new List<ReopenRequest>();

        public AttendanceRecord()
        {
            Remarks = string.Empty;
            Status = AttendanceStatus.Draft;
            Version = 0;
        }

        public AttendanceRecord(Guid studentId, Guid courseId, DateTime attendanceDate) : this()
        {
            StudentId = studentId;
            CourseId = courseId;
            AttendanceDate = attendanceDate;
        }

        /// <summary>
        /// Create a correction version of this record
        /// </summary>
        public AttendanceRecord CreateCorrectionVersion()
        {
            var correction = new AttendanceRecord
            {
                StudentId = this.StudentId,
                CourseId = this.CourseId,
                AttendanceDate = this.AttendanceDate,
                Status = AttendanceStatus.Draft,
                Version = this.Version + 1,
                ParentVersionId = this.Id,
                IsPresent = this.IsPresent,
                Remarks = this.Remarks
            };

            return correction;
        }
    }
}
