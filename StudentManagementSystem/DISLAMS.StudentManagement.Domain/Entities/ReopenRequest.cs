namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Represents a request to reopen a submitted/approved attendance record
    /// 
    /// Workflow:
    /// 1. Record is in Submitted or Approved state
    /// 2. Teacher or Admin requests reopen
    /// 3. Record state changes to "ReopenRequested"
    /// 4. Admin approves reopen request
    /// 5. Record goes back to Draft
    /// 6. Teacher can now make corrections
    /// </summary>
    public class ReopenRequest : Entity
    {
        /// <summary>
        /// Reference to the attendance record
        /// </summary>
        public Guid AttendanceRecordId { get; set; }
        public virtual AttendanceRecord AttendanceRecord { get; set; }

        /// <summary>
        /// Reason for requesting reopen
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Who requested the reopen
        /// </summary>
        public Guid RequestedBy { get; set; }

        /// <summary>
        /// When the reopen was requested
        /// </summary>
        public DateTime RequestedAt { get; set; }

        /// <summary>
        /// Current status (Pending, Approved, Rejected)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Who approved/rejected the reopen request
        /// </summary>
        public Guid? ApprovedBy { get; set; }

        /// <summary>
        /// When the reopen request was approved/rejected
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Comments from the approver
        /// </summary>
        public string ApprovalComments { get; set; }

        public ReopenRequest()
        {
            Reason = string.Empty;
            Status = "Pending";
            ApprovalComments = string.Empty;
            RequestedAt = DateTime.UtcNow;
        }

        public ReopenRequest(
            Guid attendanceRecordId,
            string reason,
            Guid requestedBy) : this()
        {
            AttendanceRecordId = attendanceRecordId;
            Reason = reason;
            RequestedBy = requestedBy;
        }
    }
}
