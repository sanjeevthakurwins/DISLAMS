using DISLAMS.StudentManagement.Domain.Entities;

namespace DISLAMS.StudentManagement.Domain.Repositories
{
    /// <summary>
    /// Repository for Audit Logs (append-only)
    /// </summary>
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        /// <summary>
        /// Get all audit logs for an attendance record
        /// </summary>
        Task<IEnumerable<AuditLog>> GetByAttendanceRecordIdAsync(Guid attendanceRecordId);

        /// <summary>
        /// Get audit logs in a date range
        /// </summary>
        Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get audit logs by actor/user
        /// </summary>
        Task<IEnumerable<AuditLog>> GetByActorIdAsync(Guid actorId);
    }
}
