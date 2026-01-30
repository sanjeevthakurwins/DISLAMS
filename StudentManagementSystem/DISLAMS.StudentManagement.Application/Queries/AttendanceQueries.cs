using MediatR;
using DISLAMS.StudentManagement.Application.DTOs;

namespace DISLAMS.StudentManagement.Application.Queries
{
    /// <summary>
    /// Get Attendance Record Query
    /// Retrieves a single attendance record with all details
    /// </summary>
    public class GetAttendanceQuery : IRequest<AttendanceDto>
    {
        public Guid AttendanceRecordId { get; set; }
    }

    /// <summary>
    /// Get Attendance by Student and Date Query
    /// </summary>
    public class GetAttendanceByStudentDateQuery : IRequest<AttendanceDto>
    {
        public Guid StudentId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public Guid CourseId { get; set; }
    }

    /// <summary>
    /// Get Student Attendance Range Query
    /// </summary>
    public class GetStudentAttendanceRangeQuery : IRequest<IEnumerable<AttendanceDto>>
    {
        public Guid StudentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Get Course Attendance for Date Query
    /// </summary>
    public class GetCourseAttendanceForDateQuery : IRequest<IEnumerable<AttendanceDto>>
    {
        public Guid CourseId { get; set; }
        public DateTime AttendanceDate { get; set; }
    }

    /// <summary>
    /// Get Attendance by Status Query
    /// </summary>
    public class GetAttendanceByStatusQuery : IRequest<IEnumerable<AttendanceDto>>
    {
        public string Status { get; set; }
    }

    /// <summary>
    /// Get All Versions of Attendance Query
    /// </summary>
    public class GetAttendanceVersionsQuery : IRequest<IEnumerable<AttendanceDto>>
    {
        public Guid StudentId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public Guid CourseId { get; set; }
    }

    /// <summary>
    /// Get Audit Trail Query
    /// </summary>
    public class GetAuditTrailQuery : IRequest<IEnumerable<DTOs.AuditLogDto>>
    {
        public Guid AttendanceRecordId { get; set; }
    }
}
