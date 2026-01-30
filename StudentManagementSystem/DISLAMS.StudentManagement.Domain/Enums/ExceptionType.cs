namespace DISLAMS.StudentManagement.Domain.Enums
{
    /// <summary>
    /// Types of exceptions/special cases in attendance
    /// </summary>
    public enum ExceptionType
    {
        /// <summary>
        /// Teacher was absent, attendance could not be marked
        /// </summary>
        TeacherAbsent = 1,

        /// <summary>
        /// Attendance submission was submitted after the deadline
        /// </summary>
        LateSubmission = 2,

        /// <summary>
        /// Parent/Guardian disputes the recorded attendance
        /// </summary>
        ParentDispute = 3,

        /// <summary>
        /// System failure prevented timely marking
        /// </summary>
        SystemFailure = 4,

        /// <summary>
        /// Other exceptional circumstance
        /// </summary>
        Other = 5
    }
}
