namespace DISLAMS.StudentManagement.Domain.Entities
{
    /// <summary>
    /// Represents a Student in the system
    /// </summary>
    public class Student : Entity
    {
        /// <summary>
        /// Student's Roll Number / ID
        /// </summary>
        public string StudentId { get; set; }

        /// <summary>
        /// Student's Full Name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Class/Grade (e.g., "10A", "8B")
        /// </summary>
        public string ClassGrade { get; set; }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Whether the student is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Related attendance records for this student
        /// </summary>
        public virtual ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

        public Student()
        {
            StudentId = string.Empty;
            FullName = string.Empty;
            ClassGrade = string.Empty;
            Email = string.Empty;
            IsActive = true;
        }

        public Student(string studentId, string fullName, string classGrade, string email) : this()
        {
            StudentId = studentId;
            FullName = fullName;
            ClassGrade = classGrade;
            Email = email;
        }
    }
}
