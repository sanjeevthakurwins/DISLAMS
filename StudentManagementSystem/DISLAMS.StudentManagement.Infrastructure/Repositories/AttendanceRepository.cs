using Microsoft.EntityFrameworkCore;
using DISLAMS.StudentManagement.Domain.Entities;
using DISLAMS.StudentManagement.Domain.Enums;
using DISLAMS.StudentManagement.Domain.Repositories;
using DISLAMS.StudentManagement.Infrastructure.Data;

namespace DISLAMS.StudentManagement.Infrastructure.Repositories
{
    /// <summary>
    /// Attendance Repository Implementation
    /// Handles all data access for attendance records
    /// </summary>
    public class AttendanceRepository : Repository<AttendanceRecord>, IAttendanceRepository
    {
        private readonly ApplicationDbContext _appContext;

        public AttendanceRepository(ApplicationDbContext context) : base(context)
        {
            _appContext = context;
        }

        public override async Task<AttendanceRecord> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<AttendanceRecord> GetByStudentDateCourseAsync(Guid studentId, DateTime date, Guid courseId)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => a.StudentId == studentId &&
                            a.AttendanceDate.Date == date.Date &&
                            a.CourseId == courseId &&
                            a.ParentVersionId == null) // Only get the original or latest version
                .OrderByDescending(a => a.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AttendanceRecord>> GetAllVersionsAsync(Guid studentId, DateTime date, Guid courseId)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => a.StudentId == studentId && 
                            a.AttendanceDate.Date == date.Date && 
                            a.CourseId == courseId)
                .OrderBy(a => a.Version)
                .ToListAsync();
        }

        public async Task<AttendanceRecord> GetLatestVersionAsync(Guid studentId, DateTime date, Guid courseId)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => a.StudentId == studentId && 
                            a.AttendanceDate.Date == date.Date && 
                            a.CourseId == courseId)
                .OrderByDescending(a => a.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByStudentAndDateRangeAsync(Guid studentId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => a.StudentId == studentId && 
                            a.AttendanceDate >= startDate && 
                            a.AttendanceDate <= endDate &&
                            a.ParentVersionId == null) // Only latest versions
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByCourseAndDateAsync(Guid courseId, DateTime date)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => a.CourseId == courseId && 
                            a.AttendanceDate.Date == date.Date &&
                            a.ParentVersionId == null) // Only latest versions
                .OrderBy(a => a.StudentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByStatusAsync(AttendanceStatus status)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AttendanceRecord>> GetByStatusesAsync(params AttendanceStatus[] statuses)
        {
            return await _dbSet
                .Include(a => a.Student)
                .Include(a => a.Course)
                .Where(a => statuses.Contains(a.Status))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
