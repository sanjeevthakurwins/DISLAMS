namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Represents a Course/Subject in the system
    /// </summary>
    public class Course : Entity
    {
        /// <summary>
        /// Course code (e.g., "MATH101")
        /// </summary>
        public string CourseCode { get; set; }

        /// <summary>
        /// Course name
        /// </summary>
        public string CourseName { get; set; }

        /// <summary>
        /// Teacher assigned to this course
        /// </summary>
        public Guid TeacherId { get; set; }

        /// <summary>
        /// Description of the course
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether the course is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Related attendance records
        /// </summary>
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        public Course()
        {
            CourseCode = string.Empty;
            CourseName = string.Empty;
            Description = string.Empty;
            IsActive = true;
        }

        public Course(string courseCode, string courseName, Guid teacherId, string description) : this()
        {
            CourseCode = courseCode;
            CourseName = courseName;
            TeacherId = teacherId;
            Description = description;
        }
    }
}
