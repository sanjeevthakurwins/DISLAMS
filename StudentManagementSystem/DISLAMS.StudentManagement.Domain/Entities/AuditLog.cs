using DISLAMS.StudentManagement.Domain.Enums;

namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Audit Log Entry
    /// 
    /// MANDATORY GOVERNANCE REQUIREMENT:
    /// Every sensitive action on attendance must be logged.
    /// Audit logs are APPEND-ONLY and NON-EDITABLE.
    /// 
    /// This ensures:
    /// - Compliance with regulatory requirements
    /// - Complete audit trail for governance
    /// - Non-repudiation (user cannot deny action)
    /// - Historical accountability
    /// </summary>
    public class AuditLog : Entity
    {
        /// <summary>
        /// Reference to the attendance record this log entry is for
        /// </summary>
        public Guid AttendanceRecordId { get; set; }
        public virtual AttendanceRecord AttendanceRecord { get; set; }

        /// <summary>
        /// The action that was performed (e.g., "Submitted", "Approved", "Corrected")
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Previous status before the action
        /// </summary>
        public AttendanceStatus? PreviousStatus { get; set; }

        /// <summary>
        /// New status after the action
        /// </summary>
        public AttendanceStatus NewStatus { get; set; }

        /// <summary>
        /// User ID who performed the action
        /// </summary>
        public Guid ActorId { get; set; }

        /// <summary>
        /// Role of the user who performed the action
        /// </summary>
        public UserRole ActorRole { get; set; }

        /// <summary>
        /// Reason or remarks for the action (required for corrections, approvals)
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Previous value (for updates)
        /// </summary>
        public string PreviousValue { get; set; }

        /// <summary>
        /// New value (for updates)
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Timestamp when action was performed (immutable)
        /// </summary>
        public DateTime ActionTimestamp { get; set; }

        /// <summary>
        /// Context/environment info (IP, session ID, etc.)
        /// </summary>
        public string ContextInfo { get; set; }

        public AuditLog()
        {
            Action = string.Empty;
            Reason = string.Empty;
            PreviousValue = string.Empty;
            NewValue = string.Empty;
            ContextInfo = string.Empty;
            ActionTimestamp = DateTime.UtcNow;
        }

        public AuditLog(
            Guid attendanceRecordId,
            string action,
            AttendanceStatus newStatus,
            Guid actorId,
            UserRole actorRole,
            string reason = "") : this()
        {
            AttendanceRecordId = attendanceRecordId;
            Action = action;
            NewStatus = newStatus;
            ActorId = actorId;
            ActorRole = actorRole;
            Reason = reason;
        }
    }
}
