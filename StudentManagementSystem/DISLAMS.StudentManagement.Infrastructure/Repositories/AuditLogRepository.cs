using Microsoft.EntityFrameworkCore;
using DISLAMS.StudentManagement.Domain.Entities;
using DISLAMS.StudentManagement.Domain.Repositories;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Audit Log Repository
    /// Append-only implementation - logs are never updated or deleted
    /// </summary>
    public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        private readonly ApplicationDbContext _appContext;

        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
            _appContext = context;
        }

        public async Task<IEnumerable<AuditLog>> GetByAttendanceRecordIdAsync(Guid attendanceRecordId)
        {
            return await _dbSet
                .Where(a => a.AttendanceRecordId == attendanceRecordId)
                .OrderBy(a => a.ActionTimestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(a => a.ActionTimestamp >= startDate && a.ActionTimestamp <= endDate)
                .OrderBy(a => a.ActionTimestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByActorIdAsync(Guid actorId)
        {
            return await _dbSet
                .Where(a => a.ActorId == actorId)
                .OrderByDescending(a => a.ActionTimestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Override to prevent updates on audit logs
        /// </summary>
        public override async Task<AuditLog> UpdateAsync(AuditLog entity)
        {
            throw new InvalidOperationException("Audit logs are immutable and cannot be updated.");
        }

        /// <summary>
        /// Override to prevent deletion of audit logs
        /// </summary>
        public override async Task DeleteAsync(Guid id)
        {
            throw new InvalidOperationException("Audit logs are immutable and cannot be deleted.");
        }
    }
}
