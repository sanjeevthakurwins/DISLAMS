namespace DISLAMS.StudentManagement.Domain.Enums
{
    /// <summary>
    /// Roles in the Attendance System
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Teacher - marks attendance, can request corrections/reopens
        /// </summary>
        Teacher = 1,

        /// <summary>
        /// Academic Coordinator / Admin - approves, publishes, manages corrections
        /// </summary>
        AcademicCoordinator = 2,

        /// <summary>
        /// School Leadership - can view reports and audit trails
        /// </summary>
        Leadership = 3
    }
}
