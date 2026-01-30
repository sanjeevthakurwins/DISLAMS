namespace DISLAMS.StudentManagement.Application.DTOs
{
    /// <summary>
    /// Attendance DTO for API responses
    /// </summary>
    public class AttendanceDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public Guid CourseId { get; set; }
        public string CourseName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string Status { get; set; }
        public bool IsPresent { get; set; }
        public string Remarks { get; set; }
        public int Version { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    /// <summary>
    /// Student DTO
    /// </summary>
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string ClassGrade { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Course DTO
    /// </summary>
    public class CourseDto
    {
        public Guid Id { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public Guid TeacherId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Audit Log DTO
    /// </summary>
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; }
        public string PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public string ActorName { get; set; }
        public string ActorRole { get; set; }
        public string Reason { get; set; }
        public DateTime ActionTimestamp { get; set; }
    }
}
