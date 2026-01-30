using DISLAMS.StudentManagement.Domain.Entities;

namespace DISLAMS.StudentManagement.Domain.Repositories
{
    /// <summary>
    /// Specialized repository for Attendance Records
    /// Includes domain-specific queries
    /// </summary>
    public interface IAttendanceRepository : IRepository<AttendanceRecord>
    {
        /// <summary>
        /// Get attendance record for a student on a specific date for a course
        /// </summary>
        Task<AttendanceRecord> GetByStudentDateCourseAsync(Guid studentId, DateTime date, Guid courseId);

        /// <summary>
        /// Get all versions of an attendance record (for a student/date/course)
        /// </summary>
        Task<IEnumerable<AttendanceRecord>> GetAllVersionsAsync(Guid studentId, DateTime date, Guid courseId);

        /// <summary>
        /// Get latest version of an attendance record
        /// </summary>
        Task<AttendanceRecord> GetLatestVersionAsync(Guid studentId, DateTime date, Guid courseId);

        /// <summary>
        /// Get all attendance for a student in a date range
        /// </summary>
        Task<IEnumerable<AttendanceRecord>> GetByStudentAndDateRangeAsync(Guid studentId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get all attendance for a course on a specific date
        /// </summary>
        Task<IEnumerable<AttendanceRecord>> GetByCourseAndDateAsync(Guid courseId, DateTime date);

        /// <summary>
        /// Get all attendance records in a specific status
        /// </summary>
        Task<IEnumerable<AttendanceRecord>> GetByStatusAsync(Enums.AttendanceStatus status);

        /// <summary>
        /// Get attendance records by multiple statuses
        /// </summary>
        Task<IEnumerable<AttendanceRecord>> GetByStatusesAsync(params Enums.AttendanceStatus[] statuses);
    }
}
