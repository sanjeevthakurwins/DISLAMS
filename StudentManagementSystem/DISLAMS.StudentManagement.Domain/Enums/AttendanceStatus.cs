namespace DISLAMS.StudentManagement.Domain.Enums
{
    /// <summary>
    /// Represents the state machine for Attendance Records
    /// Draft → Submitted → Approved → Published → Locked
    /// Special states: Reopen Requested, Corrected (new version)
    /// </summary>
    public enum AttendanceStatus
    {
        /// <summary>
        /// Initial state - Teacher is still marking attendance, not yet submitted
        /// </summary>
        Draft = 1,

        /// <summary>
        /// Submitted by teacher - waiting for approval
        /// No longer editable by teacher
        /// </summary>
        Submitted = 2,

        /// <summary>
        /// Approved by Academic Coordinator - verified but not yet published
        /// Can still request corrections/reopens at this stage
        /// </summary>
        Approved = 3,

        /// <summary>
        /// Published - locked for normal use
        /// Can only be corrected through official correction workflow
        /// </summary>
        Published = 4,

        /// <summary>
        /// Final locked state - no further changes allowed
        /// Only for records that have been finalized
        /// </summary>
        Locked = 5,

        /// <summary>
        /// Teacher or Admin requested to reopen the submitted/approved attendance
        /// Awaiting approval to reopen
        /// </summary>
        ReopenRequested = 6,

        /// <summary>
        /// A new version has been created as a correction
        /// Previous version remains immutable
        /// </summary>
        Corrected = 7
    }
}
